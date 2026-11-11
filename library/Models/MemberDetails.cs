using System.Collections.Generic;

namespace library.Models;

// The member's page shows two things: the member, and their loans.
// This class wraps both so the view can receive them in a single object.
public class MemberDetails
{
    public Member Member { get; set; } = new();
    public List<LoanView> Loans { get; set; } = new();
}