//
// Modified work Copyright 2017 Secure Decisions, a division of Applied Visions, Inc.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace OpenCover.Framework.Model
{
    /// <summary>
    /// An instrumentable point
    /// </summary>
    public class InstrumentationPoint
    {
        private static int _instrumentPoint;
        private static readonly object LockObject = new object();
        private static readonly List<InstrumentationPoint> InstrumentPoints;

        static InstrumentationPoint()
        {
            _instrumentPoint = 0;
            InstrumentPoints = new List<InstrumentationPoint>(8192) {null};
        }

        internal static void Clear()
        {
            InstrumentPoints.Clear();
            InstrumentPoints.Add(null);
            _instrumentPoint = 0;
        }

        internal static void ResetAfterLoading()
        {
            var points = InstrumentPoints
                .Where(x => x != null)
                .GroupBy(x => x.UniqueSequencePoint)
                .Select(g => g.OrderBy(x => x.OrigSequencePoint).First())
                .ToList();

            var max = (int)points.Max(x => x.UniqueSequencePoint);
            
            InstrumentPoints.Clear();
            InstrumentPoints.Add(null);

            for (var i = 1; i <= max; i++)
            {
                var point = new SequencePoint();
                InstrumentPoints[i] = point;
                point.UniqueSequencePoint = (uint)i;
            }

            foreach (var instrumentationPoint in points)
            {
                InstrumentPoints[(int)instrumentationPoint.UniqueSequencePoint] = instrumentationPoint;
            }

            _instrumentPoint = max;
        }

        /// <summary>
        /// Return the number of visit points
        /// </summary>
        public static int Count {
            get { return InstrumentPoints.Count; }
        }

        /// <summary>
        /// Get the number of recorded visit points for this identifier
        /// </summary>
        /// <param name="spid">the sequence point identifier - NOTE 0 is not used</param>
        public static int GetVisitCount(uint spid)
        {
            return InstrumentPoints[(int) spid].VisitCount;
        }

        /// <summary>
        /// Gets the method where this instrumentation point is defined.
        /// </summary>
        /// <param name="spid">the sequence point identifier - NOTE 0 is not used</param>
        /// <returns>Method where instrumentation point is defined</returns>
        public static Method GetDeclaringMethod(uint spid)
        {
            return spid >= InstrumentPoints.Count ? null : InstrumentPoints[(int) spid]?.DeclaringMethod;
        }

        /// <summary>
        /// Gets source location associated with instrumentation point.
        /// </summary>
        /// <returns>A tuple consisting of optional source location.</returns>
        public static Tuple<int?,int?,int?, int?> GetSourceLocation(uint spid)
        {
            return InstrumentPoints[(int) spid].GetSourceLocation();
        }

        /// <summary>
        /// Add a number of recorded visit ppints against this identifier
        /// </summary>
        /// <param name="spid">the sequence point identifier - NOTE 0 is not used</param>
        /// <param name="trackedMethodId">the id of a tracked method - Note 0 means no method currently tracking</param>
        /// <param name="amount">the number of visit points to add</param>
        public static bool AddVisitCount(uint spid, uint trackedMethodId, int amount)
        {
            return AddVisitCount(spid, Guid.Empty, trackedMethodId, amount);
        }

        /// <summary>
        /// Add a number of recorded visit ppints against this identifier
        /// </summary>
        /// <param name="spid">the sequence point identifier - NOTE 0 is not used</param>
        /// <param name="contextId">context identifier associated with code coverage</param>
        /// <param name="trackedMethodId">the id of a tracked method - Note 0 means no method currently tracking</param>
        /// <param name="amount">the number of visit points to add</param>
        public static bool AddVisitCount(uint spid, Guid contextId, uint trackedMethodId, int amount)
        {
            if (spid != 0 && spid < InstrumentPoints.Count)
            {
                var point = InstrumentPoints[(int) spid];
                point.VisitCount += amount;
                if (point.VisitCount < 0)
                {
                    point.VisitCount = int.MaxValue;
                }
                if (trackedMethodId != 0)
                {
                    AddOrUpdateTrackingPoint(trackedMethodId, amount, point);
                }

                if (contextId == Guid.Empty)
                {
                    return true;
                }

                var contextVisit = point.GetContextVisit(contextId);
                contextVisit.VisitCount++;

                return true;
            }
            return false;
        }

        private static void AddOrUpdateTrackingPoint(uint trackedMethodId, int amount, InstrumentationPoint point)
        {
            point._tracked = point._tracked ?? new List<TrackedMethodRef>();
            var tracked = point._tracked.Find(x => x.UniqueId == trackedMethodId);
            if (tracked == null)
            {
                tracked = new TrackedMethodRef {UniqueId = trackedMethodId, VisitCount = amount};
                point._tracked.Add(tracked);
            }
            else
            {
                tracked.VisitCount += amount;
                if (tracked.VisitCount < 0)
                    tracked.VisitCount = int.MaxValue;
            }            
        }

        private List<TrackedMethodRef> _tracked;

		private readonly Dictionary<Guid, ContextVisit> _contextVisitsByGuid = new Dictionary<Guid, ContextVisit>();

        /// <summary>
        /// Initialise
        /// </summary>
        public InstrumentationPoint()
        {
            lock (LockObject)
            {
                UniqueSequencePoint = (uint)++_instrumentPoint;
                InstrumentPoints.Add(this);
                OrigSequencePoint = UniqueSequencePoint;
            }
        }

        /// <summary>
        /// Gets the context visit data for a given context identifier.
        /// </summary>
        /// <param name="contextId">The identifier associated with a specific context.</param>
        /// <returns>The visit object containing a visit count associated with the context identifier.</returns>
        public ContextVisit GetContextVisit(Guid contextId)
        {
	        if (_contextVisitsByGuid.TryGetValue(contextId, out var contextVisit))
	        {
		        return contextVisit;
	        }

            contextVisit = new ContextVisit { ContextId = contextId };
	        _contextVisitsByGuid[contextId] = contextVisit;

			return contextVisit;
        }

        /// <summary>
        /// Gets line numbers associated with instrumentation point.
        /// </summary>
        /// <returns>A tuple consisting of optional start and end line.</returns>
        public virtual Tuple<int?, int?, int?, int?> GetSourceLocation()
        {
            return null;
        }

        /// <summary>
        /// Store the number of visits
        /// </summary>
        [XmlAttribute("vc")]
        public int VisitCount { get; set; }

        /// <summary>
        /// A unique number
        /// </summary>
        [XmlAttribute("uspid")]
        public UInt32 UniqueSequencePoint { get; set; }

        /// <summary>
        /// An order of the point within the method
        /// </summary>
        [XmlAttribute("ordinal")]
        public UInt32 Ordinal { get; set; }

        /// <summary>
        /// The IL offset of the point
        /// </summary>
        [XmlAttribute("offset")]
        public int Offset { get; set; }

        /// <summary>
        /// Used to hide an instrumentation point
        /// </summary>
        [XmlIgnore]
        public bool IsSkipped { get; set; }

        /// <summary>
        /// Method where the line(s) of this instrumentation point are defined.
        /// </summary>
        [XmlIgnore]
        public Method DeclaringMethod { get; set; }
        
        /// <summary>
        /// The list of tracked methods
        /// </summary>
        public TrackedMethodRef[] TrackedMethodRefs
        {
            get
            {
                return _tracked?.ToArray();
            }
            set
            {
                _tracked = null;
                if (value == null) 
                    return;
                _tracked = new List<TrackedMethodRef>(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlIgnore]
        public UInt32 OrigSequencePoint { get; set; }
    }
}