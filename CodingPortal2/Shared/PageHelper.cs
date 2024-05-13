using Microsoft.AspNetCore.Mvc.ViewFeatures;
namespace CodingPortal2.Shared;

public abstract class PageHelper
{
    public static void SetTempDataSuccessMessage(string? message, ITempDataDictionary tempData)
    {
        tempData["SuccessMessage"] = message;
    }
        
    public static void SetTempDataErrorMessage(string message, ITempDataDictionary tempData)
    {
        tempData["ErrorMessage"] = message;
    }
    
    public static void GetTempDataMessagesAndSetToViewData(ITempDataDictionary tempData, ViewDataDictionary viewData)
    {
        if (tempData["SuccessMessage"] != null)
        {
            viewData["SuccessMessage"] = tempData["SuccessMessage"];
        }
        else if (tempData["ErrorMessage"] != null)
        {
            viewData["ErrorMessage"] = tempData["ErrorMessage"];
        }
    }
}