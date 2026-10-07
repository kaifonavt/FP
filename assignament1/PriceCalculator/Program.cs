public enum TicketType
{
    Standard,
    Vip,
}
public enum DayType
{
    Weekday,
    Weekend,
}

public class Program
{
    static decimal ApplyPricingRule(    //IMPORTANT NO CHANGE
        decimal price, 
        Func<decimal, decimal> rule) => 
        rule(price);
    
    static decimal GetAgeDiscount(int age, bool isStudent)
    {
        if (age < 6) return  1.0m;
        if (age <12) return 0.50m;
        if (isStudent) return 0.15m;
        if (age >= 60) return 0.30m;
        return 0.0m;
    }

    public static decimal GetTicketPrice(decimal price, int age, bool isSudent, TicketType ticketType, DayType dayType)
    {
        Func<decimal, decimal> AgeRule = price => price*(1.00m-GetAgeDiscount(age, isSudent));
        Func<decimal, decimal> TicketTypeRule = price => (ticketType == TicketType.Vip) ? price * 1.25m : price;
        Func<decimal, decimal> DayTypeRule = price => (dayType == DayType.Weekend) ? price * 1.10m : price;
        
        decimal priceByAge = ApplyPricingRule(price, AgeRule);
        decimal priceByTicketType = ApplyPricingRule(priceByAge, TicketTypeRule);
        decimal priceByDayType = ApplyPricingRule(priceByTicketType, DayTypeRule);
        decimal priceFinal = Math.Max(0.00m, Math.Round(priceByDayType, 2, MidpointRounding.AwayFromZero));
        return priceFinal;
    }

    public static void Main(string[] args)//input output main
    {
        Console.WriteLine("Ticket Price Calculator");
        Console.Write("Enter base price: ");
        string? basePriceInput = Console.ReadLine();
        if (!decimal.TryParse(basePriceInput, out decimal basePrice) || basePrice <= 0)
        {
            Console.WriteLine("Invalid base price");
            return;
        }
        Console.Write("Enter age: ");
        string? ageInput = Console.ReadLine();
        if (!int.TryParse(ageInput, out int age) || age <= 0)
        {
            Console.WriteLine("Invalid age");
            return;
        }
        Console.Write("Student status (true/false): ");
        string? studentStatusInput = Console.ReadLine();
        if (!bool.TryParse(studentStatusInput, out bool studentStatus))
        {
            Console.WriteLine("Invalid student status");
            return;
        }
        Console.Write("Enter ticket type (Vip/Standard): ");
        string? ticketTypeInput = Console.ReadLine();
        if (!Enum.TryParse(ticketTypeInput, out TicketType ticketType) || !Enum.IsDefined(typeof(TicketType), ticketType))
        {
            Console.WriteLine("Invalid ticket type");
            return;
        }
        Console.Write("Enter day type (Weekday/Weekend): ");
        string? dayTypeInput = Console.ReadLine();
        if (!Enum.TryParse(dayTypeInput, out DayType dayType) || !Enum.IsDefined(typeof(DayType), dayType))
        {
            Console.WriteLine("Invalid day type");
            return;
        }
        decimal result = GetTicketPrice(basePrice, age, studentStatus, ticketType, dayType);
        Console.WriteLine("Final price: "+Convert.ToString(result));
    }
}