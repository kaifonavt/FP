public enum TicketType
{
    Standard,
    Vip
}

public enum DayType
{
    Weekday,
    Weekend
}

public class Program
{
    private static decimal ApplyPricingRule( //IMPORTANT NO CHANGE
        decimal price,
        Func<decimal, decimal> rule)
    {
        return rule(price);
    }

    private static decimal GetAgeDiscount(int age, bool isStudent)
    {
        if (age < 6) return 1.0m;
        if (age < 12) return 0.50m;
        if (isStudent) return 0.15m;
        if (age >= 60) return 0.30m;
        return 0.0m;
    }

    public static decimal GetTicketPrice(decimal price, int age, bool isSudent, TicketType ticketType, DayType dayType)
    {
        Func<decimal, decimal> AgeRule = price => price * (1.00m - GetAgeDiscount(age, isSudent));
        Func<decimal, decimal> TicketTypeRule = price => ticketType == TicketType.Vip ? price * 1.25m : price;
        Func<decimal, decimal> DayTypeRule = price => dayType == DayType.Weekend ? price * 1.10m : price;

        var priceByAge = ApplyPricingRule(price, AgeRule);
        var priceByTicketType = ApplyPricingRule(priceByAge, TicketTypeRule);
        var priceByDayType = ApplyPricingRule(priceByTicketType, DayTypeRule);
        var priceFinal = Math.Max(0.00m, Math.Round(priceByDayType, 2, MidpointRounding.AwayFromZero));
        return priceFinal;
    }

    public static void Main(string[] args) //input output main
    {
        Console.WriteLine("Ticket Price Calculator");
        Console.Write("Enter base price: ");
        var basePriceInput = Console.ReadLine();
        if (!decimal.TryParse(basePriceInput, out var basePrice) || basePrice <= 0)
        {
            Console.WriteLine("Invalid base price");
            return;
        }

        Console.Write("Enter age: ");
        var ageInput = Console.ReadLine();
        if (!int.TryParse(ageInput, out var age) || age <= 0)
        {
            Console.WriteLine("Invalid age");
            return;
        }

        Console.Write("Student status (true/false): ");
        var studentStatusInput = Console.ReadLine();
        if (!bool.TryParse(studentStatusInput, out var studentStatus))
        {
            Console.WriteLine("Invalid student status");
            return;
        }

        Console.Write("Enter ticket type (Vip/Standard): ");
        var ticketTypeInput = Console.ReadLine();
        if (!Enum.TryParse(ticketTypeInput, out TicketType ticketType) ||
            !Enum.IsDefined(typeof(TicketType), ticketType))
        {
            Console.WriteLine("Invalid ticket type");
            return;
        }

        Console.Write("Enter day type (Weekday/Weekend): ");
        var dayTypeInput = Console.ReadLine();
        if (!Enum.TryParse(dayTypeInput, out DayType dayType) || !Enum.IsDefined(typeof(DayType), dayType))
        {
            Console.WriteLine("Invalid day type");
            return;
        }

        var result = GetTicketPrice(basePrice, age, studentStatus, ticketType, dayType);
        Console.WriteLine("Final price: " + Convert.ToString(result));
    }
}