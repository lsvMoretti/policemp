namespace BotWorker.Services
{
    public interface IArgumentProviderService
    {
        string[] GetArguments();
    }

    public class ArgumentProviderService : IArgumentProviderService
    {
        private readonly string[] _args;

        public ArgumentProviderService(string[] args)
        {
            _args = args;
        }

        public string[] GetArguments() => _args;
    }
}