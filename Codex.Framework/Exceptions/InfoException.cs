﻿using System;

namespace Codex.Core.Exceptions
{
    [Serializable]
    public class InfoException : Exception
    {
        public InfoException(string message, string? code) : base(message)
        {
            Code = code;
        }

        public string? Code { get; init; }
    }
}
