using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenPkgRepo;

public class Session
{
    public string Id;
    public readonly DateTime ExpiryDate;
    public readonly int OwnerID;
    private static string _base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public Session(int idLength, DateTime expiryDate, int owner)
    {
        
        for (int i = 0; i < idLength; i++)
        {
            Id += _base62[RandomNumberGenerator.GetInt32(62)];
        }
        
        ExpiryDate = expiryDate;
        OwnerID = owner;
    }
    public bool HasExpired()
    {
        return DateTime.Now.CompareTo(ExpiryDate) > 0;
    }
    public AccountInfo GetAccount()
    {
        return AccountHandler.GetAccountFromId(OwnerID);
    }
    public async Task<AccountInfo> GetAccountAsync()
    {
        return await AccountHandler.GetAccountFromIdAsync(OwnerID);
    }
}