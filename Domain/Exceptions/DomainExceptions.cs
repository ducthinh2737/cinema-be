using System;
using System.Collections.Generic;

namespace CinemaBooking.API.Domain.Exceptions
{
    /// <summary>
    /// Represents errors that occur due to business rules violations.
    /// Maps to BadRequest (400) in Global Exception Middleware.
    /// </summary>
    public class BusinessException : InvalidOperationException
    {
        public BusinessException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents errors that occur when a resource is not found.
    /// Maps to NotFound (404) in Global Exception Middleware.
    /// </summary>
    public class NotFoundException : KeyNotFoundException
    {
        public NotFoundException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents errors that occur during data validation.
    /// Maps to BadRequest (400) in Global Exception Middleware.
    /// </summary>
    public class ValidationException : ArgumentException
    {
        public ValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// Represents errors that occur due to invalid requests.
    /// Maps to BadRequest (400) in Global Exception Middleware.
    /// </summary>
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message) { }
    }
}
