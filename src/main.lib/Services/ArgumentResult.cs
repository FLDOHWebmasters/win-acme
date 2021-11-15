﻿using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class ArgumentResult<TResult>
    {
        /// <summary>
        /// Metadata obtained through reflection
        /// </summary>
        private readonly CommandLineAttribute _metaData;

        /// <summary>
        /// Starting value from command line parser
        /// </summary>
        protected readonly TResult? _argumentValue;

        /// <summary>
        /// Default value set at some point during the chain
        /// </summary>
        protected TResult? _defaultValue;

        /// <summary>
        /// User input set at some point during the chain
        /// </summary>
        protected TResult? _inputValue;

        /// <summary>
        /// Label to show to the user in interactive mode
        /// </summary>
        protected string? _inputLabel;

        /// <summary>
        /// Description to show to the user in interactive mode
        /// </summary>
        protected string? _inputDescription;

        /// <summary>
        /// Required value
        /// </summary>
        private bool _inputMultiline = false;


        /// <summary>
        /// Allow null input from interactive mode
        /// </summary>
        protected bool _allowEmpty;

        /// <summary>
        /// Logservice
        /// </summary>
        protected ILogService _log;

        /// <summary>
        /// Inputservice available (e.g. interactive mode)
        /// </summary>
        protected IInputService? _inputService;

        /// <summary>
        /// Ask the user for input
        /// </summary>
        private readonly Func<ArgumentResultInputArguments<TResult>, Task<TResult?>> _inputFunction;

        /// <summary>
        /// Validator to run
        /// </summary>
        private readonly List<(Func<TResult, Task<bool>> Validate, Func<TResult, string> Failed)> _validators = new();

        /// <summary>
        /// Do not emit default value
        /// </summary>
        private bool _defaultAsNull = false;

        /// <summary>
        /// Required value
        /// </summary>
        private bool _required = false;

        /// <summary>
        /// Test if we currently have a value
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        private bool HasValue(TResult? current)
        {
            if (current == null)
            {
                return false;
            }
            else if (current is string currentString)
            {
                if (_allowEmpty)
                {
                    return true;
                } 
                else
                {
                    return !string.IsNullOrWhiteSpace(currentString);
                }
            }
            else if (current is ProtectedString protectedString)
            {
                if (protectedString.Value == null)
                {
                    return false;
                }
                else if (_allowEmpty)
                {
                    return true;
                }
                else
                {
                    return !string.IsNullOrWhiteSpace(protectedString.Value);
                }
            }
            return true;
        }

        internal ArgumentResult(
            TResult baseValue, 
            CommandLineAttribute metaData, 
            Func<ArgumentResultInputArguments<TResult>, Task<TResult?>> input, 
            ILogService log,
            bool allowEmpty = false)
        {
            _argumentValue = baseValue;
            _metaData = metaData;
            _inputFunction = input;
            _allowEmpty = allowEmpty;
            _log = log;
        }

        internal class ArgumentResultInputArguments<TDefault>
        {
            public string Label { get; set; } = "Input";
            public TDefault? Default { get; set; }
            public bool Required { get; set; } = false;
            public bool Multiline { get; set; } = false;
        }

        /// <summary>
        /// Allow interactive input
        /// </summary>
        /// <param name="input"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public ArgumentResult<TResult> Interactive(
            IInputService input, 
            string? label = null, 
            string? description = null,
            bool multiline = false)
        {
            _inputService = input;
            _inputLabel = label;
            _inputMultiline = multiline;
            _inputDescription = description;
            return this;
        }

        /// <summary>
        /// Test the value using the validator parameter,
        /// throwing an exception with errorReason if the
        /// validator returns false
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="errorReason"></param>
        /// <returns></returns>
        public ArgumentResult<TResult> Validate(Func<TResult, Task<bool>> validator, string errorReason) => Validate(validator, x => errorReason);
        public ArgumentResult<TResult> Validate(Func<TResult, Task<bool>> validator, Func<TResult, string> failed)
        {
            _validators.Add(new(validator, failed));
            return this;
        }

        /// <summary>
        /// Shortcut for required input validation
        /// </summary>
        /// <returns></returns>
        public ArgumentResult<TResult> Required(bool required = true)
        {
            _required = required;
            return this;
        }

        /// <summary>
        /// Set a default value if not value was
        /// specified so far
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ArgumentResult<TResult> WithDefault(TResult value)
        {
            _defaultValue = value;
            return this;
        }

        /// <summary>
        /// Set a default value if not value was
        /// specified so far
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ArgumentResult<TResult> DefaultAsNull()
        {
            _defaultAsNull = true;
            return this;
        }

        private async Task<TResult?> GetInput(IInputService input, TResult? current)
        {
            input.CreateSpace();
            input.Show("Description", _inputDescription ?? _metaData.Description);
            if (HasValue(_defaultValue))
            {
                var showValue = _metaData.Secret ? "********" : _defaultValue?.ToString();
                input.Show("Default", showValue);
            }
            if (HasValue(_argumentValue))
            {
                var showValue = _metaData.Secret ? "********" : _argumentValue?.ToString();
                input.Show("Argument", showValue);
            }
            var args = new ArgumentResultInputArguments<TResult>()
            {
                Label = _inputLabel ?? _metaData.Name,
                Default = current,
                Required = _required,
                Multiline = _inputMultiline
            };
            return await _inputFunction(args);
        }

        private async Task<(bool, string?)> IsValid(TResult? returnValue)
        {
            // Validate
            if (_required && !HasValue(returnValue))
            {
                if (!string.IsNullOrWhiteSpace(_inputLabel))
                {
                    return (false, "this is a required value");
                } 
                else
                {
                    return (false, $"missing --{_metaData.ArgumentName}");
                }
            }
            if (HasValue(returnValue))
            {
                foreach (var validator in _validators)
                {
                    var validationResult = false;
                    try
                    {
                        validationResult = await validator.Validate(returnValue!);
                    }
                    catch (Exception ex)
                    {
                        _log.Debug(ex.ToString());
                        var errorReason = validator.Failed(returnValue!);
                        return (false, $"failed --{_metaData.ArgumentName}: {errorReason}");
                    }
                    if (!validationResult)
                    {
                        var errorReason = validator.Failed(returnValue!);
                        if (!string.IsNullOrWhiteSpace(_inputLabel))
                        {
                            return (false, $"Invalid input: {errorReason}");
                        }
                        else
                        {
                            return (false, $"invalid --{_metaData.ArgumentName}: {errorReason}");
                        }
                    }
                }
            }
            return (true, null);
        }

        /// <summary>
        /// Run the chain of methods required to 
        /// get the final value
        /// </summary>
        /// <returns></returns>
        public async Task<TResult?> GetValue()
        {
            var returnValue = _argumentValue;
            if (!HasValue(returnValue))
            {
                returnValue = _defaultValue;
            }
            while (true)
            {
                if (_inputService != null)
                {
                    _inputValue = await GetInput(_inputService, _defaultValue);
                    if (HasValue(_inputValue))
                    {
                        returnValue = _inputValue;
                    }
                }
                var (valid, validationError) = await IsValid(returnValue);
                if (valid)
                {
                    break;
                }
                else if (_inputService == null)
                    {
                        throw new Exception(validationError);
                    } 
                    else
                    {
                        _log.Error(validationError ?? "Error");
                    }
            }

            // Sometime we don't want to store the default result
            // even when it comes straight from the user
            if (_defaultAsNull && Comparer<TResult>.Default.Compare(returnValue, _defaultValue) == 0)
            {
                return default;
            }
            return returnValue;
        }
    }
}