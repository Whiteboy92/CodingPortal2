#include <iostream>
#include <vector>
int findMax(const std::vector<int>& array) {    if (array.empty()) {        return 0;    }    int max = array[0];    for (size_t i = 1; i < array.size(); ++i) {        if (array[i] > max) {            max = array[i];        }    }    return max;}int main() {    std::vector<int> array;    int num;    while (std::cin >> num) {        array.push_back(num);    }    if (array.empty()) {        std::cout << "" << std::endl;        return 1;    }    int result = findMax(array);    std::cout << result << std::endl;    return 0;}
