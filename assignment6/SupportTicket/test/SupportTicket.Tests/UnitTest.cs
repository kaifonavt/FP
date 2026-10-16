using Xunit;

namespace SupportTicket.Tests
{
    public class TicketWorkflowTests
    {
        [Fact]
        public void Create_ValidInputs_ReturnsValidTicketWithCorrectData()
        {
            var user = User.Create(1, "John", "Doe");
            var result = Ticket.Create(100, "Fix database", user, Level.High);
            Assert.IsType<ValidTicket>(result); 
            var validTicket = (ValidTicket)result;
            Assert.Equal(100, validTicket.Ticket.id);
            Assert.Equal("Fix database", validTicket.Ticket.description);
        }
        [Fact]
        public void Create_NegativeId_ReturnsInvalidTicketWithMessage()
        {
            var user = User.Create(1, "John", "Doe");
            var result = Ticket.Create(-5, "Fix bug", user, Level.Basic);

            Assert.IsType<InvalidTicket>(result);
            var invalidTicket = (InvalidTicket)result;
            Assert.Equal("Ticket ID must be greater than zero.", invalidTicket.Error);
        }
        [Fact]
        public void Create_BoundaryIdOne_ReturnsValidTicket()
        {
            var user = User.Create(1, "John", "Doe");
            var result = Ticket.Create(1, "Boundary test", user, Level.Basic);

            Assert.IsType<ValidTicket>(result);
        }
        [Fact]
        public void Evaluate_ValidTicket_CalculatesCorrectScore()
        {
            var user = User.Create(1, "John", "Doe");
            var ticketResult = Ticket.Create(150, "Network issue", user, Level.Medium);
            var validTicket = (ValidTicket)ticketResult;

            int score = Ticket.Evaluate(validTicket.Ticket);

            Assert.Equal(30, score);
        }
        [Fact]
        public void PatternMatching_HandlesValidAndInvalidCasesCorrectly()
        {
            var user = User.Create(1, "John", "Doe");
            var validResult = Ticket.Create(10, "Valid", user, Level.Basic);
            var invalidResult = Ticket.Create(0, "Invalid", user, Level.Basic);
            string ProcessResult(TicketResult res) => res switch
            {
                ValidTicket v => $"Success: {v.Ticket.id}",
                InvalidTicket i => $"Error: {i.Error}",
                _ => throw new InvalidOperationException("Unknown state")
            };

            Assert.Equal("Success: 10", ProcessResult(validResult));
            Assert.Equal("Error: Ticket ID must be greater than zero.", ProcessResult(invalidResult));
        }
        [Fact]
        public void Create_NullUser_ReturnsInvalidTicket()
        {
            var result = Ticket.Create(10, "No user", null!, Level.Basic);
            Assert.IsType<InvalidTicket>(result);
            var invalidTicket = (InvalidTicket)result;
            Assert.Equal("User cannot be null.", invalidTicket.Error); 
        }
        [Fact]
        public void Evaluate_RepeatedCall_ReturnsSameScore()
        {
            var user = User.Create(1, "John", "Doe");
            var ticket = ((ValidTicket)Ticket.Create(250, "Server down", user, Level.High)).Ticket;
            int firstCall = Ticket.Evaluate(ticket);
            int secondCall = Ticket.Evaluate(ticket);

            Assert.Equal(firstCall, secondCall);
        }
        [Fact]
        public void UserIsValid_InvalidId_DoesNotModifyUsersList()
        {
            var users = new List<User>
            {
                User.Create(1, "John", "Doe"),
                User.Create(2, "Jane", "Smith")
            };
            int originalCount = users.Count;

            bool isValid = User.IsValid(99, users);
            Assert.False(isValid);
            Assert.Equal(originalCount, users.Count);
            Assert.Equal(1, users[0].id); 
        }
    }
}