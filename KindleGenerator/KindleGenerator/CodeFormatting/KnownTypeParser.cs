using System.Collections.Generic;

namespace KindleGenerator.CodeFormatting
{
    public class KnownTypeParser : WordWrapParserBase
    {
        private readonly IEnumerable<string> _knownTypes;

        private static readonly string[] _defaultKnownTypes = new[]
                                                            {
                                                                //TODO validate that "BooleanDisposable" is still a type.
                                                                "Console", "Application", "AppDomain",
                                                                "Exception", "IOException", "TimeoutException",
                                                                "IDisposable", "Disposable", "BooleanDisposable", "CompositeDisposable", "ContextDisposable", "MultipleAssignmentDisposable", "SerialDisposable", "SingleAssignmentDisposable",
                                                                "Func", "Action", "Unit",
                                                                "IEquatable", "IEqualityComparer",
                                                                "IEnumerable", "Enumerable", "EnumerableEx", "IDictionary", "ILookup",
                                                                "Thread", "Timer", "TimeSpan", 
                                                                "IObserver", "IObservable", "Observable", "ISubject", "Subject", "ReplaySubject", "AsyncSubject", "BehaviorSubject", 
                                                                "IScheduler", "Scheduler", "TestScheduler", "ScheduledItem","Notifcation", "NotificationKind",
                                                                "Mock", "Assert", "TestInitialize", "TestMethod",
                                                                "EventArgs", "PropertyChangedEventHandler", "PropertyChangedEventArgs", "FirstChanceExceptionEventArgs",
                                                                "IAsyncResult", "AsyncCallback",
                                                                "Task", "CancellationToken", 
                                                                "IEventPatternSource", "IEventSource", "EventPattern", "EventHandler",
                                                            };

        public KnownTypeParser()
            :this(_defaultKnownTypes)
        {
            
        }

        public KnownTypeParser(IEnumerable<string> knownTypes)
        {
            _knownTypes = knownTypes;
        }

        public override string CssClass
        {
            get { return "knownType"; }
        }

        public override IEnumerable<string> Words
        {
            get { return _knownTypes; }
        }
    }
}