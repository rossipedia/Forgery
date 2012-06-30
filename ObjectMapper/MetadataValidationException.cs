// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetadataValidationException.cs" company="Bryan Ross">
//   Copyright (c) Bryan Ross. No rights reserved.
// </copyright>
// <summary>
//   Defines the MetadataValidationException type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ObjectMapper
{
    using System;

    /// <summary>
    /// Thrown when an error is detected in the Metadata for a type.
    /// </summary>
    public class MetadataValidationException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataValidationException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MetadataValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataValidationException" /> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. 
        /// If the <paramref name="innerException" /> parameter is not a null reference (Nothing in Visual Basic), 
        /// the current exception is raised in a catch block that handles the inner exception.</param>
        public MetadataValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}