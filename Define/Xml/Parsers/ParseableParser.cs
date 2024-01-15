using JetBrains.Annotations;

namespace Define.Xml.Parsers
{
    /// <summary>
    /// Handles all types that implement <see cref="IParsable{TSelf}"/>.
    /// This includes most primitive types.
    /// </summary>
    public sealed class ParseableParser : XmlParser
    {
        private readonly Dictionary<Type, IWorker> workers = [];
        
        /// <inheritdoc/>
        public override bool CanParseNoContext => true;

        /// <summary>
        /// Creates a new <see cref="ParseableParser"/> and sets its
        /// <see cref="XmlParser.Priority"/> to -100.
        /// </summary>
        public ParseableParser()
        {
            Priority = -100;
        }
        
        /// <inheritdoc/>
        public override bool CanHandle(Type type)
        {
            foreach (var i in type.GetInterfaces())
            {
                if (i.IsConstructedGenericType && typeof(IParsable<>).IsAssignableFrom(i.GetGenericTypeDefinition()))
                    return true;
            }
            return false;
        }

        private IWorker GetWorker(Type type)
        {
            if (workers.TryGetValue(type, out var found))
                return found;

            if (Activator.CreateInstance(typeof(Worker<>).MakeGenericType(type)) is not IWorker instance)
                throw new Exception($"Failed to make generic worker class for type {type.FullName}!");
            
            workers.Add(type, instance);
            return instance;
        }

        /// <inheritdoc/>
        public override object? Parse(in XmlParseContext context)
        {
            if (GetWorker(context.TargetType).TryParse(context.TextValue, null, out var parsed))
                return parsed;

            throw new Exception($"Failed to parse '{context.TextValue}' as a {context.TargetType} using IParser<> method.");
        }

        private interface IWorker
        {
            bool TryParse(string txt, IFormatProvider? fmt, out object? parsed);
        }

        [UsedImplicitly]
        private sealed class Worker<T> : IWorker where T : IParsable<T>
        {
            public bool TryParse(string txt, IFormatProvider? fmt, out object? parsed)
            {
                if (T.TryParse(txt, fmt, out var raw))
                {
                    parsed = raw;
                    return true;
                }
                parsed = null;
                return false;
            }
        }
    }
}
