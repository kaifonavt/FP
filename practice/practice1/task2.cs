namespace Task2;

public class task2
{
    //base func1
    double defaultSpeed = 80;
    double calculateDistance(double hours)
    {
            return defaultSpeed * hours;
    }
    //rewrite
    double calculateDistancePure(double hours, double speed)
    {
        return speed*hours;
    }
    //base func2
    private string language = "en";
    srting getMessage(bool completed)
    {
        if (language == "en")
            return completed ? "Completed" : "In progress";
        return completed ? "Zaversheno" : "V progresse";
    }
    //rewrite 
    string getMessage(string language, bool completed)
    {
        
    }

    {}
}