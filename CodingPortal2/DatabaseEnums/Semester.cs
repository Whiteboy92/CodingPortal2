namespace CodingPortal2.DatabaseEnums;

public enum Semester
{
    Winter,
    Summer
}

public static class SemesterHelper
{
    public static Semester GetSemester()
    {
        int currentMonth = DateTime.Now.Month;
        
        return currentMonth is >= 3 and <= 8 ?
            Semester.Summer : Semester.Winter;
    }
}