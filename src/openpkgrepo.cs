using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using OakLogger;

namespace OpenPkgRepo;

public enum ErrorCode
{
    UnknownError,
    InvalidCredentials,
    NameAlreadyTaken,
    NoItemFound,
    ItemNotCreated,
    BadConfiguration,
    InvalidImage,
    MustBeSquare,
    ExpiredSession,
    InvalidSession
}

public static class PkgRepo
{
    public static OLog LogSuccess { get; private set; }
    public static OLog LogWarning { get; private set; }
    public static OLog LogError { get; private set; }
    public static OLog LogInfo { get; private set; }
    public static SqliteConnection Database { get; private set; }
    public static Dictionary<string, object> Configuration { get; private set; }
    private static string _configPath = "config.jsonc";
    public static readonly List<Session> Sessions = new();
    public static readonly Dictionary<ErrorCode, JObject> ErrorResponse = new(){
        {ErrorCode.UnknownError, BuildErrorMsg(0, "An unknown error occurred.")},
        {ErrorCode.InvalidCredentials, BuildErrorMsg(1, "Invalid credentials!")},
        {ErrorCode.NameAlreadyTaken, BuildErrorMsg(2, "This username has already been taken!")},
        {ErrorCode.NoItemFound, BuildErrorMsg(3, "No item were found that satisfies the given query.")},
        {ErrorCode.ItemNotCreated, BuildErrorMsg(4,"This item has yet to be created.")},
        {ErrorCode.BadConfiguration, BuildErrorMsg(5,"The owner of this server has not properly configured the server.")},
        {ErrorCode.InvalidImage, BuildErrorMsg(6,"The provided file is not a valid image!.")},
        {ErrorCode.MustBeSquare, BuildErrorMsg(7,"The provided image is not 1:1 aspect ratio, the server requires a square image!.")},
        {ErrorCode.ExpiredSession, BuildErrorMsg(8,"This session has expired and can no longer be used.")},
        {ErrorCode.InvalidSession, BuildErrorMsg(9,"Could not find the session.")}
    };

    public static async Task Main(string[] args)
    {
        
        
        // CommandLine setup
        var programRootCommand = new RootCommand("Open source .NET server.");
        var configPathOption = new Option<string>(new [] { "--config", "-c" }, "Specify a configuration file to use.");
        configPathOption.SetDefaultValue(_configPath);
        configPathOption.ArgumentHelpName = "file path";
        configPathOption.ArgumentHelpName = "directory path";
        var clearConfigOption = new Option<bool>(new [] { "--resetconfig", "-r" }, "Resets the configuration file.");

        programRootCommand.AddOption(clearConfigOption);
        programRootCommand.AddOption(configPathOption);
        programRootCommand.SetHandler((clearConfigValue, configPathValue) =>
        {
            _configPath = configPathValue;
            if (clearConfigValue && File.Exists(_configPath))
            {
                File.Delete(_configPath);
            }
            InitServer();
        }, clearConfigOption, configPathOption);


        await programRootCommand.InvokeAsync(args);

    }
    /// <summary>
    /// Starts the server
    /// </summary>    
    private static void InitServer()
    {
        // Logger utilities.
        LogSuccess = BuildLogger("SUCCESS", new(0, 175, 0));
        LogWarning = BuildLogger("WARNING", new(255, 204, 0));
        LogError = BuildLogger("ERROR", new(250, 0, 0));
        LogInfo = BuildLogger("INFO", new	(84, 175, 190));
        // Load config.
        if (!File.Exists(_configPath))
        {
            File.WriteAllText(_configPath, GenerateBlankConfigurationFile());
        }

        Configuration = JObject.Parse(File.ReadAllText(_configPath)).ToObject<Dictionary<string, object>>();
        if (!Directory.Exists((string)Configuration["PfpPath"]))
        {
            Directory.CreateDirectory((string)Configuration["PfpPath"]);
        }
        // Load databases.
        Database = new("Data Source=OpenPkgRepo.db");
        Database.Open();
        LogSuccess.Print("Loaded Database.");
        using (var command = Database.CreateCommand())
        {
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Accounts (AccountsID INTEGER PRIMARY KEY,Username, Password, Location, BirthDate, Bio) ";
            command.ExecuteNonQuery();
        } 
        // Server WebServer = new Server("*", Convert.ToInt32(Configuration["Port"]), false);
        var webServer = new OPRServer(IPAddress.Any, Convert.ToInt32(Configuration["Port"]));
        webServer.Start();

        LogSuccess.Print("Server is running!");
        Console.ReadLine();
    }
    /// <summary>
    /// Function to simplify the creation of OLogs.
    /// </summary>
    /// <param name="loggerType">
    /// The type of the logger usually something related to the context of the message.
    /// </param>
    /// <param name="color">
    /// RGB value for the background color.
    /// </param>
    /// <returns>
    /// An OLog logger that has a stylized output.
    /// </returns>
    public static OLog BuildLogger(string loggerType, Tuple<byte, byte, byte> color)
    {
        var standardLogItems = new List<LogItem> { LogItem.Type, LogItem.TimeSinceStartup };
        var standardLogOutput = new LogOutput(Console.Out, useColor: true);
        
        OLog logger = new()
        {
            LogType = loggerType,
            Color = color
        };
        logger.Outputs.Add(standardLogOutput);
        logger.LogItems.InsertRange(0, standardLogItems);
        return logger;
    }

    private static readonly List<(string, string, string)> _defaultConfigValue = new()
    {
        ( "Port", "1433", "The port used by the server" ),
        ( "PfpPath", "\"pfp\"", "unused" ),
        ( "SessionExpirationDate", "1.0", "The amount of hours required for a session to expire." )
    };
    private static string GenerateBlankConfigurationFile()
    {
        string output = "// Please make a backup of this file when updating OpenPkgRepo.\n" +
                        "{\n";
        for (int i = 0; i < _defaultConfigValue.Count; i++)
        {
            output +=
                $"\t\"{_defaultConfigValue[i].Item1}\" : {_defaultConfigValue[i].Item2}{(i != _defaultConfigValue.Count ? "," : "")} // {_defaultConfigValue[i].Item3}\n";
        }
        output += "}";
        return output;
    }
    private static JObject BuildErrorMsg(int errorCode, string errorMessage)
    {
        JObject returnObject = new()
        {
            ["InternalErrorCode"] = errorCode,
            ["ErrorMessage"] = errorMessage
        };
        return returnObject;
    }
}

// Thanks aradalvand from StackOverflow for this secure hash function.
public static class Hash
{
    private const int _saltSize = 16; // 128 bits
    private const int _keySize = 24; // 256 bits
    private const int _iterations = 100000;
    private static readonly HashAlgorithmName _algorithm = HashAlgorithmName.SHA256;

    private const char _segmentDelimiter = ':';
    /// <summary>
    /// Turns a string into a Pbkdf2 hash.
    /// </summary>
    /// <param name="input">
    /// String input.
    /// </param>
    /// <returns>
    /// Pbkdf2 hash.
    /// </returns>
    public static string Create(string input)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(_saltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            input,
            salt,
            _iterations,
            _algorithm,
            _keySize
        );
        return string.Join(
            _segmentDelimiter,
            Convert.ToHexString(hash),
            Convert.ToHexString(salt),
            _iterations,
            _algorithm
        );
    }
    /// <summary>
    /// See if a text string matches the pbkdf2 hash.
    /// </summary>
    /// <param name="input">
    /// Standard text.
    /// </param>
    /// <param name="hashString">
    /// The pbkdf2 hash.
    /// </param>
    /// <returns>
    /// A boolean value indicating if the input matches the hash
    /// </returns>
    public static bool Verify(string input, string hashString)
    {
        string[] segments = hashString.Split(_segmentDelimiter);
        byte[] hash = Convert.FromHexString(segments[0]);
        byte[] salt = Convert.FromHexString(segments[1]);
        int iterations = int.Parse(segments[2]);
        HashAlgorithmName algorithm = new HashAlgorithmName(segments[3]);
        byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(
            input,
            salt,
            iterations,
            algorithm,
            hash.Length
        );
        return CryptographicOperations.FixedTimeEquals(inputHash, hash);
    }
}
