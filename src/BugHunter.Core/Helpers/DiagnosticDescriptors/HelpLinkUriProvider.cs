﻿using System;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace BugHunter.Core.Helpers.DiagnosticDescriptors
{
    /// <summary>
    /// Helper class for providing URI links for analyzers
    /// </summary>
    public static class HelpLinkUriProvider
    {
        private static readonly string ONLINE_DOCUMENTATION_URL = @"http://kentico.github.io/bug-hunter/{0}";
        private static readonly Regex ANALYZER_ID_FORMAT = new Regex(@"^BH[0-9a-zA-Z]{4}$", RegexOptions.None);

        /// <summary>
        /// Returns an online documentation help link URI based on <param name="analyzerId"></param>
        /// </summary>
        /// <param name="analyzerId">ID of an analyzer to provide an URI for, must be non empty and in BHXXXX, where X is alphanumeric</param>
        /// <returns>URI with an online documentation for given analyzer ID, if ID is in correct format; throws otherwise</returns>
        public static string GetHelpLink(string analyzerId)
        {
            if (string.IsNullOrWhiteSpace(analyzerId))
            {
                throw new ArgumentException($"The {nameof(analyzerId)} must not be empty.", nameof(analyzerId));
            }

            if (analyzerId.Length != 6 || !ANALYZER_ID_FORMAT.IsMatch(analyzerId))
            {
                throw new ArgumentException($"The {nameof(analyzerId)} must be in format 'BHXXXX' where X are alphanumeric characters.", nameof(analyzerId));
            }

            return string.Format(ONLINE_DOCUMENTATION_URL, analyzerId);
        }
    }
}