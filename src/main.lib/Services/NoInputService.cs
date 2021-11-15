using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class NoInputService : IInputService
    {
        private readonly ILogService _log;
        public NoInputService(ILogService log) => _log = log;
        public Task<TResult> ChooseFromMenu<TResult>(string what, List<Choice<TResult>> choices, Func<string, Choice<TResult>>? unexpected = null) => Task.FromResult(choices.First().Item);
        public Task<TResult?> ChooseOptional<TSource, TResult>(string what, IEnumerable<TSource> options, Func<TSource, Choice<TResult?>> creator, string nullChoiceLabel) where TResult : class => Task.FromResult<TResult?>(default);
        public Task<TResult> ChooseRequired<TSource, TResult>(string what, IEnumerable<TSource> options, Func<TSource, Choice<TResult>> creator) => Task.FromResult(creator(options.First()).Item);
        public void CreateSpace() { }
        public string FormatDate(DateTime date) => date.ToString("M/d");
        public Task<bool> PromptYesNo(string message, bool defaultOption) => Task.FromResult(defaultOption);
        public Task<string?> ReadPassword(string what) => Task.FromResult<string?>(default);
        public Task<string> RequestString(string what, bool multiline = false) => Task.FromResult(string.Empty);
        public void Show(string? label, string? value = null, int level = 0) => _log.Information($"{label}: {value}");
        public Task<bool> Wait(string message = "Press <Enter> to continue") => Task.FromResult(true);
        public async Task WritePagedList(IEnumerable<Choice> listItems) => await Task.Run(() => listItems.ToList().ForEach(x => _log.Information(x.Description ?? string.Empty)));
    }
}
