namespace Task1;
public class task1
{
    //Task 1. Calculate travel time
   //Write two functions that calculate travel time based on distance and speed.
    double speed = 60.0;
    double CalculateTime(double distance)
    {
        return distance / speed;//relate on global var
    }
    double CalculateTimePure(double distance, double speed)
    {
        return distance / speed;//vars inputed in funcs
    }
    //Task 2. Validate a username
    //Write two functions that checks whether a username is reserved.
    List<string> reservedNames = new() { "artur", "kaifoanvt" };
    bool IsReservedImpure(string username)
    {
        return reservedNames.Contains(username.ToLower());//list out of fucn
    }
    bool IsReservedPure(string username, IEnumerable<string> reservedList)
    {
        return reservedList.Contains(username, StringComparer.OrdinalIgnoreCase);//list inside 
    }
    //Task 3. Generate the next student ID
    //Write two functions that generates next student ID, based on previous student ID.
    int previousId
    int generateIdpPure()
    {
        return previousId + 1;//global var
    }
    int generateIdInure(int previousId)
    {
        return previousId + 1;//id insede the func 
    }
    //Task 4. Convert a score to a grade
    //Write two functions that converts a 100 point grade into a letter grade.
    char convertScore(int score)
    {
        switch (score)//idk how to make it inpure fr
        {
            case  >= 90:
                return 'A';
            case >= 75:
                return 'B';
            case >= 60:
                return 'C';
            case  >= 50:
                return 'D';
            default:
                return 'F';
        }
    }
    char ConvertGradePure(int score) => score switch
    {
        >= 90 => 'A',
        >= 75 => "B",
        >= 60 => 'C',
        >= 50 => 'D',
        _ => 'C'
    };
    
}