namespace ConsoleApp9;

class Program
{
    static void Main(string[] args)
    {
        List<int> numbers = new List<int>();
        
        while (true)
        {
            string input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
            {
                break;
            }
            
            string[] numbersAsString = input.Split( );
            
            foreach (string numStr in numbersAsString)
            {
                if (int.TryParse(numStr, out int num))
                {
                    numbers.Add(num);
                }
                else
                {
                    Console.WriteLine($"Invalid input: {numStr}");
                }
            }
        }

        if (numbers.Count > 0)
        {
            int max = numbers[0];
            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] > max)
                {
                    max = numbers[i];
                }
            }
            
            Console.WriteLine(max);
        }
        else
        {
            Console.WriteLine("No numbers were entered.");
        }
    }
}

