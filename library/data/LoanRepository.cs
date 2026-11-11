using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using library.Models;

namespace library.Data;

// Every SQL statement about loans lives in this one class.
public class LoanRepository
{
    // The library's rule: a member may have at most 3 books out at once.
    public const int MaxOpenLoans = 3;

    private readonly NpgsqlDataSource _dataSource;

    public LoanRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // ---------------------------------------------------------
    // HELPER: Copies the current row into a LoanView.
    // Positions 0 to 7 match every loan SELECT in this file.
    // ---------------------------------------------------------
    private static LoanView ReadLoan(NpgsqlDataReader reader)
    {
        return new LoanView
        {
            LoanId = reader.GetInt64(0),
            MemberId = reader.GetInt64(1),
            MemberName = reader.GetString(2),
            BookTitle = reader.GetString(3),
            LoanedOn = reader.GetFieldValue<DateOnly>(4),
            ReturnedOn = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateOnly>(5),
            DaysAllowed = reader.GetInt32(6),
            Status = reader.GetString(7)
        };
    }

    // ---------------------------------------------------------
    // TASK 7: READ every loan, joined with member and book.
    // ---------------------------------------------------------
    public async Task<List<LoanView>> GetAllAsync()
    {
        const string sql =
            "SELECT loan_id, l.member_id, m.full_name, b.title, l.loaned_on, l.returned_on, l.days_allowed, " +
            "  CASE WHEN l.returned_on IS NOT NULL THEN 'Returned' " +
            "       WHEN l.loaned_on + l.days_allowed < CURRENT_DATE THEN 'Overdue' " +
            "       ELSE 'Out' END " +
            "FROM lending.loan l " +
            "JOIN lending.member m ON l.member_id = m.member_id " +
            "JOIN lending.book b ON l.book_id = b.book_id " +
            "ORDER BY l.loaned_on DESC, l.loan_id DESC;";

        var loans = new List<LoanView>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(ReadLoan(reader));
        }

        return loans;
    }

    // ---------------------------------------------------------
    // TASK 8: BORROW a book inside a transaction.
    // Checks if the member is at the limit. If so, ROLLBACK.
    // If below the limit, INSERT, then COMMIT.
    // ---------------------------------------------------------
    public async Task<bool> BorrowAsync(long memberId, long bookId, int daysAllowed)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        // 1. Lock this member's row so two simultaneous borrows don't 
        //    read the same count at the same time.
        await using (var lockMember = new NpgsqlCommand(
            "SELECT member_id FROM lending.member WHERE member_id = @member_id FOR UPDATE;", connection, transaction))
        {
            lockMember.Parameters.AddWithValue("member_id", memberId);
            await lockMember.ExecuteNonQueryAsync();
        }

        // 2. Count how many books this member currently has out.
        long openLoans;
        await using (var count = new NpgsqlCommand(
            "SELECT COUNT(*) FROM lending.loan WHERE member_id = @member_id AND returned_on IS NULL;", connection, transaction))
        {
            count.Parameters.AddWithValue("member_id", memberId);
            openLoans = Convert.ToInt64(await count.ExecuteScalarAsync());
        }

        // 3. If they are at the limit, undo and return false.
        if (openLoans >= MaxOpenLoans)
        {
            await transaction.RollbackAsync();
            return false;
        }

        // 4. Otherwise, insert the new loan.
        await using (var insert = new NpgsqlCommand(
            "INSERT INTO lending.loan (member_id, book_id, days_allowed) VALUES (@member_id, @book_id, @days_allowed);", connection, transaction))
        {
            insert.Parameters.AddWithValue("member_id", memberId);
            insert.Parameters.AddWithValue("book_id", bookId);
            insert.Parameters.AddWithValue("days_allowed", daysAllowed);
            await insert.ExecuteNonQueryAsync();
        }

        // 5. Save permanently.
        await transaction.CommitAsync();
        return true;
    }

    // ---------------------------------------------------------
    // TASK 9: RETURN a book.
    // Sets returned_on to today, but ONLY if it's currently NULL.
    // ---------------------------------------------------------
    public async Task ReturnAsync(long loanId)
    {
        const string sql = "UPDATE lending.loan SET returned_on = CURRENT_DATE " +
                           "WHERE loan_id = @id AND returned_on IS NULL;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", loanId);
        await command.ExecuteNonQueryAsync();
    }

    // ---------------------------------------------------------
    // TASK 10: READ one member's loans (for the Details page).
    // Same as GetAllAsync, but filtered by memberId.
    // ---------------------------------------------------------
    public async Task<List<LoanView>> GetByMemberAsync(long memberId)
    {
        const string sql =
            "SELECT loan_id, l.member_id, m.full_name, b.title, l.loaned_on, l.returned_on, l.days_allowed, " +
            "  CASE WHEN l.returned_on IS NOT NULL THEN 'Returned' " +
            "       WHEN l.loaned_on + l.days_allowed < CURRENT_DATE THEN 'Overdue' " +
            "       ELSE 'Out' END " +
            "FROM lending.loan l " +
            "JOIN lending.member m ON l.member_id = m.member_id " +
            "JOIN lending.book b ON l.book_id = b.book_id " +
            "WHERE l.member_id = @member_id " +
            "ORDER BY l.loaned_on DESC, l.loan_id DESC;";

        var loans = new List<LoanView>();
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("member_id", memberId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(ReadLoan(reader));
        }

        return loans;
    }
}