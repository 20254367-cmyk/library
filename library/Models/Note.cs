namespace library.Models;

// One Note object holds one row of lending.note.
public class Note
{
    public long NoteId { get; set; }          // note_id BIGINT, the primary key
    public string Title { get; set; } = "";   // title VARCHAR(100) NOT NULL
    public string? Body { get; set; }         // body TEXT, may be NULL
    // created_on is left out: the database fills it automatically
}