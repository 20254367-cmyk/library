using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using library.Models;

namespace library.Data;

public class BookRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public BookRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // READ: every book in the library
    public async Task<List<Book>> GetAllAsync()
    {
        const string sql =
            "SELECT book_id, title, category, price " +
            "FROM lending.book " +
            "ORDER BY title;";

        var books = new List<Book>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            books.Add(new Book
            {
                BookId = reader.GetInt64(0),
                Title = reader.GetString(1),
                Category = reader.IsDBNull(2) ? null : reader.GetString(2),
                Price = reader.IsDBNull(3) ? null : reader.GetDecimal(3)
            });
        }

        return books;
    }

    // READ: one book by id
    public async Task<Book?> GetByIdAsync(long id)
    {
        const string sql =
            "SELECT book_id, title, category, price " +
            "FROM lending.book " +
            "WHERE book_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Book
            {
                BookId = reader.GetInt64(0),
                Title = reader.GetString(1),
                Category = reader.IsDBNull(2) ? null : reader.GetString(2),
                Price = reader.IsDBNull(3) ? null : reader.GetDecimal(3)
            };
        }

        return null;
    }

    // CREATE
    public async Task AddAsync(Book book)
    {
        const string sql =
            "INSERT INTO lending.book (title, category, price) " +
            "VALUES (@title, @category, @price);";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("title", book.Title);
        command.Parameters.AddWithValue("category", (object?)book.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("price", (object?)book.Price ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    // UPDATE
    public async Task UpdateAsync(Book book)
    {
        const string sql =
            "UPDATE lending.book " +
            "SET title = @title, category = @category, price = @price " +
            "WHERE book_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("title", book.Title);
        command.Parameters.AddWithValue("category", (object?)book.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("price", (object?)book.Price ?? DBNull.Value);
        command.Parameters.AddWithValue("id", book.BookId);

        await command.ExecuteNonQueryAsync();
    }

    // DELETE
    public async Task DeleteAsync(long id)
    {
        const string sql = "DELETE FROM lending.book WHERE book_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);

        await command.ExecuteNonQueryAsync();
    }
}