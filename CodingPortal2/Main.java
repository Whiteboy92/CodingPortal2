import java.util.ArrayList;import java.util.List;import java.util.Scanner;public class Main {    public static int findMax(List<Integer> array) {        int max = array.get(0);        for (int i = 1; i < array.size(); ++i) {            if (array.get(i) > max) {                max = array.get(i);            }        }        return max;    }    public static void main(String[] args) {        List<Integer> array = new ArrayList<>();        Scanner scanner = new Scanner(System.in);        while (scanner.hasNextInt()) {            int num = scanner.nextInt();            array.add(num);        }        int result = findMax(array);        System.out.println(result);    }}