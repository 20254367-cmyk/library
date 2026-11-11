using library.Models;
using Npgsql;


namespace library.Data;


// Every SQL statement about members lives in this one class.
public class MemberRepository
{
    private readonly NpgsqlDataSource _dataSource;


    public MemberRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }


    // Copies the current row of the reader into a Member.
    // Positions 0 to 3 match the column order in both SELECTs.
    private static Member ReadMember(NpgsqlDataReader reader)
    {
        return new Member
        {
            MemberId = reader.GetInt64(0),
            FullName = reader.GetString(1),
            Email = reader.IsDBNull(2) ? null : reader.GetString(2),
            MemberType = reader.GetString(3)
        };
    }
    // READ: every member, sorted by full name.
    // READ: every member, with how many books each has out now.
    public async Task<List<Member>> GetAllAsync()
    {
        const string sql =
            "SELECT m.member_id, m.full_name, m.email, m.member_type, COUNT(l.loan_id) " +
            "FROM lending.member m " +
            "LEFT JOIN lending.loan l ON l.member_id = m.member_id AND l.returned_on IS NULL " +
            "GROUP BY m.member_id " +
            "ORDER BY m.full_name;";


        var members = new List<Member>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var member = ReadMember(reader);
            member.OpenLoans = reader.GetInt64(4);
            members.Add(member);
        }
        return members;
    }
    // CREATE: insert a new member. The database chooses member_id and fills join_date.
    public async Task AddAsync(Member member)
    {
        const string sql = "INSERT INTO lending.member (full_name, email, member_type) " +
                           "VALUES (@full_name, @email, @member_type);";


        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("full_name", member.FullName);
        // A C# null must be sent as DBNull.Value, which is how a database NULL is written.
        command.Parameters.AddWithValue("email", (object?)member.Email ?? DBNull.Value);
        // MemberType is NOT NULL, so no DBNull.Value here.
        command.Parameters.AddWithValue("member_type", member.MemberType);


        await command.ExecuteNonQueryAsync();           // runs SQL that returns no rows
    }
    // READ: one member, or null when no member has that id.
    public async Task<Member?> GetByIdAsync(long id)
    {
        const string sql = "SELECT member_id, full_name, email, member_type FROM lending.member WHERE member_id = @id;";


        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);      // @id in the SQL receives this value


        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadMember(reader) : null;
    }


    // UPDATE: change every editable column of one member.
    public async Task UpdateAsync(Member member)
    {
        const string sql = "UPDATE lending.member " +
                           "SET full_name = @full_name, email = @email, member_type = @member_type " +
                           "WHERE member_id = @id;";


        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("full_name", member.FullName);
        command.Parameters.AddWithValue("email", (object?)member.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("member_type", member.MemberType);
        command.Parameters.AddWithValue("id", member.MemberId);


        await command.ExecuteNonQueryAsync();
    }
    // DELETE: remove one member.
    public async Task DeleteAsync(long id)
    {
        const string sql = "DELETE FROM lending.member WHERE member_id = @id;";


        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);


        await command.ExecuteNonQueryAsync();
    }
}