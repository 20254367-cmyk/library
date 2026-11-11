using System.ComponentModel.DataAnnotations;

namespace library.Models;

// One Member object holds one row of lending.member.
public class Member
{
    public long MemberId { get; set; } // member_id BIGINT, the primary key

    // 1. Rule: a name of at most 80 characters.
    [StringLength(80, ErrorMessage = "The name can be at most 80 characters.")]
    // 2. Rule: the name is written as Lastname, Firstname.
    [RegularExpression(@"^[A-Za-z' -]+, [A-Za-z' -]+$", ErrorMessage = "Write the name as Lastname, Firstname.")]
    public string FullName { get; set; } = ""; // full_name VARCHAR(80) NOT NULL

    // 3. Rule: an email of at most 120 characters.
    [StringLength(120, ErrorMessage = "The email can be at most 120 characters.")]
    // 4. Rule: an email such as ana@ubaguio.edu, when one is given.
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$", ErrorMessage = "Enter an email such as ana@ubaguio.edu.")]
    public string? Email { get; set; } // email VARCHAR(120), may be NULL, UNIQUE

    public string MemberType { get; set; } = ""; // member_type VARCHAR(10) NOT NULL

    // Filled only by the member list; never saved.
    public long OpenLoans { get; set; }
}