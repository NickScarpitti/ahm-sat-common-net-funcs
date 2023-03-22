﻿using System.Collections.Concurrent;
using Common_Net_Funcs.Tools;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Common_Net_Funcs.Web;

/// <summary>
/// For use with ASP.NET Core ModelStateDictionary
/// </summary>
public static class ModelErrorHelpers
{
    /// <summary>
    /// Convert ModelStateDictionary used by ASP.NET Core into a standard dictionary
    /// </summary>
    /// <param name="modelState"></param>
    /// <returns></returns>
    public static Dictionary<string, string?> ParseModelStateErrors(ModelStateDictionary modelState)
    {
        Dictionary<string, string?> errors = new();
        foreach (string modelStateKey in modelState.Keys)
        {
            ModelStateEntry? value = modelState[modelStateKey];
            if (value != null && value.Errors.Count > 0)
            {
                string? errText = null;
                foreach (ModelError error in value.Errors)
                {
                    errText += error.ErrorMessage + "";
                }
                if (errText.Right(1) == ".")
                {
                    errText = errText![0..^1]; //Removes last character
                }
                errors.Add(modelStateKey, errText);
            }
        }
        errors.Add("", "Invalid model state");
        return errors;
    }

    /// <summary>
    /// Convert ModelStateDictionary used by ASP.NET Core into a standard dictionary
    /// </summary>
    /// <param name="modelState"></param>
    /// <returns></returns>
    public static ConcurrentDictionary<string, string?> ParseModelStateErrorsConcurrent(ModelStateDictionary modelState)
    {
        ConcurrentDictionary<string, string?> errors = new();
        foreach (string modelStateKey in modelState.Keys)
        {
            ModelStateEntry? value = modelState[modelStateKey];
            if (value != null && value.Errors.Count > 0)
            {
                string? errText = null;
                foreach (ModelError error in value.Errors)
                {
                    errText += error.ErrorMessage + "";
                }
                if (errText.Right(1) == ".")
                {
                    errText = errText![0..^1]; //Removes last character
                }
                errors.AddDictionaryItem(modelStateKey, errText);
            }
        }
        errors.AddDictionaryItem("", "Invalid model state");
        return errors;
    }
}