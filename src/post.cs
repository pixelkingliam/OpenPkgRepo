using System.Collections.Generic;
using System.Threading.Tasks;
using OpenPkgRepo.Routes;
using Pixel.OakLog;

namespace OpenPkgRepo;
class Post
{
    public int Owner;
    public int Id;
    public string Name;
    public string Description;
    public string Body; // MD format
    public List<int> PkgIds;
    public List<Comment> Comments;

}
class Comment
{
    int Owner;
    int ParentPostId;

    int Id;
}
static class PostHandler
{
    public static OLog PostLogger = OLog.Create("POST", (219, 128, 78));
    public static async void CreatePost(Post post)
    {
        await using var command = PkgRepo.Database.CreateCommand();
        command.CommandText =
            """

                                INSERT INTO Posts (Owner, Name, Description, Body)
                                VALUES ($Owner, $Name, $Description, $Body)

                """;
        command.Parameters.AddWithValue("$Owner", post.Owner);
        command.Parameters.AddWithValue("$Name", post.Name);
        command.Parameters.AddWithValue("$Description", post.Description);
        command.Parameters.AddWithValue("$Body", post.Body);
        await command.ExecuteNonQueryAsync();
    }
    public static List<Post> GetAllPost(int owner)
    {
        List<Post> list = new();
                using var command = PkgRepo.Database.CreateCommand();
        command.CommandText =
            @"
                SELECT *
                FROM Posts
                WHERE Owner = $Owner
                ";
        command.Parameters.AddWithValue("$Owner", owner);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if(reader.FieldCount !>= 4)
            {
                PostLogger.Print("[DEBUG] Seemingly malformed Posts database?!");
            }
            Post post = new();
            /*if ((string)reader.GetValue(0) == username)
            {
                return true;
            }*/
            
            post.Id = (int)reader.GetValue(0);
            post.Owner = (int)reader.GetValue(1);
            if (post.Owner != owner)
            {
                PostLogger.Print("[DEBUG] GetAllPost failed!");
            }
            post.Name = (string)reader.GetValue(2);
            post.Description = (string)reader.GetValue(3);
            post.Body = (string)reader.GetValue(4);
            list.Add(post);
        }
        return list;

    }
}