﻿// <copyright file="ConsoleDriverException.cs" company="Jim Evans">
//
// Copyright 2014 Jim Evans
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ConsoleDriverApplication
{
    /// <summary>
    /// Exception thrown when a drive is incapable of performing the requested command.
    /// </summary>
    [Serializable]
    public class ConsoleDriverException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleDriverException"/> class.
        /// </summary>
        public ConsoleDriverException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleDriverException"/> class with 
        /// a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConsoleDriverException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleDriverException"/> class with
        /// a specified error message and a reference to the inner exception that is the
        /// cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception,
        /// or <see langword="null"/> if no inner exception is specified.</param>
        public ConsoleDriverException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleDriverException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized 
        /// object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual 
        /// information about the source or destination.</param>
        protected ConsoleDriverException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}