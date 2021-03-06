﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Com.PhilChuang.Utils.MvvmNotificationChainer
{
    public delegate void NotificationChainCallback (Object sender, String notifyingProperty, String dependentProperty);

    /// <summary>
    /// Defines a NotificationChain. Observes multiple notifying properties and triggers callbacks for the dependent property.
    /// </summary>
    public class NotificationChain : IDisposable
    {
        /// <summary>
        /// The Manager that publishes to this chain.
        /// </summary>
        public NotificationChainManager ParentManager { get; set; }

        /// <summary>
        /// Name of the property that depends on other properties (e.g. Cost depends on Quantity and Price)
        /// </summary>
        public String DependentPropertyName { get; private set; }

        private List<String> myObservedPropertyNames = new List<String> ();

        /// <summary>
        /// The properties being observed by this chain
        /// </summary>
        public IEnumerable<String> ObservedPropertyNames
        {
            get { return myObservedPropertyNames.Select(s => s); }
        }

        private List<Regex> myObservedRegexes = new List<Regex> ();

        /// <summary>
        /// The property regexes being observed by this chain
        /// </summary>
        public IEnumerable<String> ObservedRegexes
        {
            get { return myObservedRegexes.Select (r => r.ToString ()); }
        }

        /// <summary>
        /// Whether or not the notification has been fully defined (if false, then modifications are still allowed)
        /// </summary>
        public bool IsFinished { get; private set; }

        public bool IsDisposed { get; private set; }

        private List<NotificationChainCallback> myCallbacks = new List<NotificationChainCallback> ();
        private NotificationChainCallback myFireCallbacksCallback;

        /// <summary>
        /// </summary>
        /// <param name="parentManager"></param>
        /// <param name="dependentPropertyName">Name of the depending property</param>
        public NotificationChain (NotificationChainManager parentManager, String dependentPropertyName)
        {
            parentManager.ThrowIfNull ("parentManager");
            dependentPropertyName.ThrowIfNull ("dependentPropertyName");

            ParentManager = parentManager;
            DependentPropertyName = dependentPropertyName;
            myFireCallbacksCallback = (sender, notifyingProperty, dependentProperty) => Execute (sender, notifyingProperty);
        }

        public void Dispose ()
        {
            if (IsDisposed) return;

            ParentManager = null;

            myObservedPropertyNames.Clear ();
            myObservedPropertyNames = null;

            myObservedRegexes.Clear ();
            myObservedRegexes = null;

            myCallbacks.Clear ();
            myCallbacks = null;

            myFireCallbacksCallback = null;

            IsDisposed = true;
        }

        /// <summary>
        /// Performs the configuration action on the current NotificationChain (if not yet Finished).
        /// </summary>
        /// <param name="configAction"></param>
        /// <returns></returns>
        public NotificationChain Configure (Action<NotificationChain> configAction)
        {
            configAction.ThrowIfNull ("configAction");

            if (IsFinished || IsDisposed) return this;

            configAction (this);

            return this;
        }

        /// <summary>
        /// Watch for any notifications on current notifying object
        /// </summary>
        /// <returns></returns>
        public NotificationChain OnAny ()
        {
            return OnRegex (@"^.*$");
        }

        /// <summary>
        /// Specifies a regex to watch for on the current notifying object
        /// </summary>
        /// <param name="regex"></param>
        /// <returns></returns>
        public NotificationChain OnRegex (String regex)
        {
            regex.ThrowIfNullOrBlank ("regex");

            if (IsFinished || IsDisposed) return this;

            if (!ObservedRegexes.Contains (regex))
                myObservedRegexes.Add (new Regex (regex));

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object.
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="propGetter"></param>
        /// <returns></returns>
        public NotificationChain On<T1> (Expression<Func<T1>> propGetter)
        {
            propGetter.ThrowIfNull ("propGetter");

            if (IsFinished || IsDisposed) return this;

            return On (propGetter.GetPropertyOrFieldName ());
        }

        /// <summary>
        /// Specifies a property name to observe on the current notifying object
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public NotificationChain On (String propertyName)
        {
            propertyName.ThrowIfNullOrBlank ("propertyName");

            if (IsFinished || IsDisposed) return this;

            if (!myObservedPropertyNames.Contains (propertyName))
                myObservedPropertyNames.Add (propertyName);

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object, and its sub-properties (T2+) to observe
        /// </summary>
        /// <typeparam name="T1">The property (T1) to observe on T0, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T2">The property (T2) to observe on T1</typeparam>
        /// <param name="prop1Getter"></param>
        /// <param name="prop2Getter"></param>
        /// <returns></returns>
        public NotificationChain On<T1, T2> (
            Expression<Func<T1>> prop1Getter,
            Expression<Func<T1, T2>> prop2Getter)
            where T1 : class, INotifyPropertyChanged
        {
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
			
            if (IsFinished || IsDisposed) return this;

            DeepOn (myFireCallbacksCallback,
                    prop1Getter,
                    prop2Getter);

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object, and its sub-properties (T2+) to observe
        /// </summary>
        /// <typeparam name="T1">The property (T1) to observe on T0, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T2">The property (T2) to observe on T1, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T3">The property (T3) to observe on T2</typeparam>
        /// <param name="prop1Getter"></param>
        /// <param name="prop2Getter"></param>
        /// <param name="prop3Getter"></param>
        /// <returns></returns>
        public NotificationChain On<T1, T2, T3> (
            Expression<Func<T1>> prop1Getter,
            Expression<Func<T1, T2>> prop2Getter,
            Expression<Func<T2, T3>> prop3Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
        {
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");
			
            if (IsFinished || IsDisposed) return this;

            DeepOn (myFireCallbacksCallback,
                    prop1Getter,
                    prop2Getter,
                    prop3Getter);

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object, and its sub-properties (T2+) to observe
        /// </summary>
        /// <typeparam name="T1">The property (T1) to observe on T0, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T2">The property (T2) to observe on T1, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T3">The property (T3) to observe on T2, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T4">The property (T4) to observe on T3</typeparam>
        /// <param name="prop1Getter"></param>
        /// <param name="prop2Getter"></param>
        /// <param name="prop3Getter"></param>
        /// <param name="prop4Getter"></param>
        /// <returns></returns>
        public NotificationChain On<T1, T2, T3, T4> (
            Expression<Func<T1>> prop1Getter,
            Expression<Func<T1, T2>> prop2Getter,
            Expression<Func<T2, T3>> prop3Getter,
            Expression<Func<T3, T4>> prop4Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
            where T3 : class, INotifyPropertyChanged
        {
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");
            prop4Getter.ThrowIfNull ("prop4Getter");
			
            if (IsFinished || IsDisposed) return this;

            DeepOn (myFireCallbacksCallback,
                    prop1Getter,
                    prop2Getter,
                    prop3Getter,
                    prop4Getter);

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object (T0)
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="propGetter"></param>
        /// <returns></returns>
        private NotificationChain On<T0, T1> (Expression<Func<T0, T1>> propGetter)
        {
            propGetter.ThrowIfNull ("propGetter");

            if (IsFinished || IsDisposed) return this;

            return On (propGetter.GetPropertyOrFieldName ());
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object, and its sub-properties (T2+) to observe
        /// </summary>
        /// <typeparam name="T1">The property (T1) to observe on T0, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T2">The property (T2) to observe on T1, implements INotifyPropertyChanged</typeparam>
        /// <param name="topLevelCallback"></param>
        /// <param name="prop1Getter"></param>
        /// <param name="prop2Getter"></param>
        /// <returns></returns>
        private NotificationChain DeepOn<T1, T2> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<T1>> prop1Getter,
            Expression<Func<T1, T2>> prop2Getter)
            where T1 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");

            if (IsFinished || IsDisposed) return this;

            On (prop1Getter);

            var mgr = ParentManager.CreateOrGetManager (prop1Getter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .On (prop2Getter)
               .AndCall (topLevelCallback);

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object (T0), and its sub-properties (T2+) to observe
        /// </summary>
        /// <typeparam name="T0">The top-level (T0) notifyingObject, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T1">The property (T1) to observe on T0, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T2">The property (T2) to observe on T1, implements INotifyPropertyChanged</typeparam>
        /// <param name="topLevelCallback"></param>
        /// <param name="prop1Getter"></param>
        /// <param name="prop2Getter"></param>
        /// <returns></returns>
        private NotificationChain DeepOn<T0, T1, T2> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<T0, T1>> prop1Getter,
            Expression<Func<T1, T2>> prop2Getter)
            where T0 : class, INotifyPropertyChanged
            where T1 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");

            if (IsFinished || IsDisposed) return this;

            On (prop1Getter);

            var mgr = ParentManager.CreateOrGetManager (prop1Getter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .On (prop2Getter)
               .AndCall (topLevelCallback);

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object, and its sub-properties (T2+) to observe
        /// </summary>
        /// <typeparam name="T1">The property (T1) to observe on T0, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T2">The property (T2) to observe on T1, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T3">The property (T3) to observe on T2</typeparam>
        /// <param name="topLevelCallback"></param>
        /// <param name="prop1Getter"></param>
        /// <param name="prop2Getter"></param>
        /// <param name="prop3Getter"></param>
        /// <returns></returns>
        private NotificationChain DeepOn<T1, T2, T3> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<T1>> prop1Getter,
            Expression<Func<T1, T2>> prop2Getter,
            Expression<Func<T2, T3>> prop3Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");

            if (IsFinished || IsDisposed) return this;

            On (prop1Getter);

            var mgr = ParentManager.CreateOrGetManager (prop1Getter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .DeepOn (topLevelCallback, prop2Getter, prop3Getter)
               .AndCall (topLevelCallback);

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object (T0), and its sub-properties (T2+) to observe
        /// </summary>
        /// <typeparam name="T0">The top-level (T0) notifyingObject, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T1">The property (T1) to observe on T0, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T2">The property (T2) to observe on T1, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T3">The property (T3) to observe on T2</typeparam>
        /// <param name="topLevelCallback"></param>
        /// <param name="prop1Getter"></param>
        /// <param name="prop2Getter"></param>
        /// <param name="prop3Getter"></param>
        /// <returns></returns>
        private NotificationChain DeepOn<T0, T1, T2, T3> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<T0, T1>> prop1Getter,
            Expression<Func<T1, T2>> prop2Getter,
            Expression<Func<T2, T3>> prop3Getter)
            where T0 : class, INotifyPropertyChanged
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");

            if (IsFinished || IsDisposed) return this;

            On (prop1Getter);

            var mgr = ParentManager.CreateOrGetManager (prop1Getter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .DeepOn (topLevelCallback, prop2Getter, prop3Getter)
               .AndCall (topLevelCallback);

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object, and its sub-properties (T2+) to observe
        /// </summary>
        /// <typeparam name="T1">The property (T1) to observe on T0, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T2">The property (T2) to observe on T1, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T3">The property (T3) to observe on T2, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T4">The property (T4) to observe on T3</typeparam>
        /// <param name="topLevelCallback"></param>
        /// <param name="prop1Getter"></param>
        /// <param name="prop2Getter"></param>
        /// <param name="prop3Getter"></param>
        /// <param name="prop4Getter"></param>
        /// <returns></returns>
        private NotificationChain DeepOn<T1, T2, T3, T4> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<T1>> prop1Getter,
            Expression<Func<T1, T2>> prop2Getter,
            Expression<Func<T2, T3>> prop3Getter,
            Expression<Func<T3, T4>> prop4Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
            where T3 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");
            prop4Getter.ThrowIfNull ("prop4Getter");

            if (IsFinished || IsDisposed) return this;

            On (prop1Getter);

            var mgr = ParentManager.CreateOrGetManager (prop1Getter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .DeepOn (topLevelCallback, prop2Getter, prop3Getter, prop4Getter)
               .AndCall (topLevelCallback);

            return this;
        }

        /// <summary>
        /// Specifies a property (T1) to observe on the current notifying object, and its sub-properties (T2+) to observe
        /// </summary>
        /// <typeparam name="T0">The top-level (T0) notifyingObject, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T1">The property (T1) to observe on T0, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T2">The property (T2) to observe on T1, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T3">The property (T3) to observe on T2, implements INotifyPropertyChanged</typeparam>
        /// <typeparam name="T4">The property (T4) to observe on T3</typeparam>
        /// <param name="topLevelCallback"></param>
        /// <param name="prop1Getter"></param>
        /// <param name="prop2Getter"></param>
        /// <param name="prop3Getter"></param>
        /// <param name="prop4Getter"></param>
        /// <returns></returns>
        private NotificationChain DeepOn<T0, T1, T2, T3, T4> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<T0, T1>> prop1Getter,
            Expression<Func<T1, T2>> prop2Getter,
            Expression<Func<T2, T3>> prop3Getter,
            Expression<Func<T3, T4>> prop4Getter)
            where T0 : class, INotifyPropertyChanged
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
            where T3 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");
            prop4Getter.ThrowIfNull ("prop4Getter");

            if (IsFinished || IsDisposed) return this;

            On (prop1Getter);

            var mgr = ParentManager.CreateOrGetManager (prop1Getter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .DeepOn (topLevelCallback, prop2Getter, prop3Getter, prop4Getter);

            return this;
        }

        public NotificationChain OnCollection<T1> (Expression<Func<ObservableCollection<T1>>> collectionPropGetter)
            where T1 : class
        {
            collectionPropGetter.ThrowIfNull ("collectionPropGetter");

            // notify when the collection object completely changes
            On (collectionPropGetter);

            // notify when the collection is modified (items added/removed)
            var mgr = ParentManager.CreateOrGetCollectionManager (collectionPropGetter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .On (CollectionNotificationChainManager.ObservedCollectionPropertyName)
               .AndCall (myFireCallbacksCallback);

            return this;
        }

        public NotificationChain OnCollection<T1, T2> (
            Expression<Func<ObservableCollection<T1>>> collectionPropGetter,
            Expression<Func<T1, T2>> prop1Getter)
            where T1 : class, INotifyPropertyChanged
        {
            collectionPropGetter.ThrowIfNull ("collectionPropGetter");
            prop1Getter.ThrowIfNull ("prop1Getter");

            if (IsFinished || IsDisposed) return this;

            DeepOnCollection (myFireCallbacksCallback, collectionPropGetter, prop1Getter);

            return this;
        }

        public NotificationChain OnCollection<T1, T2, T3> (
            Expression<Func<ObservableCollection<T1>>> collectionPropGetter,
            Expression<Func<T1, T2>> prop1Getter,
            Expression<Func<T2, T3>> prop2Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
        {
            collectionPropGetter.ThrowIfNull ("collectionPropGetter");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");

            if (IsFinished || IsDisposed) return this;

            DeepOnCollection (myFireCallbacksCallback, collectionPropGetter, prop1Getter, prop2Getter);

            return this;
        }

        public NotificationChain OnCollection<T1, T2, T3, T4> (
            Expression<Func<ObservableCollection<T1>>> collectionPropGetter,
            Expression<Func<T1, T2>> prop1Getter,
            Expression<Func<T2, T3>> prop2Getter,
            Expression<Func<T3, T4>> prop3Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
            where T3 : class, INotifyPropertyChanged
        {
            collectionPropGetter.ThrowIfNull ("collectionPropGetter");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");

            if (IsFinished || IsDisposed) return this;

            DeepOnCollection (myFireCallbacksCallback, collectionPropGetter, prop1Getter, prop2Getter, prop3Getter);

            return this;
        }

        public NotificationChain OnCollection<T1, T2, T3, T4, T5> (
            Expression<Func<ObservableCollection<T1>>> collectionPropGetter,
            Expression<Func<T1, T2>> prop1Getter,
            Expression<Func<T2, T3>> prop2Getter,
            Expression<Func<T3, T4>> prop3Getter,
            Expression<Func<T4, T5>> prop4Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
            where T3 : class, INotifyPropertyChanged
            where T4 : class, INotifyPropertyChanged
        {
            collectionPropGetter.ThrowIfNull ("collectionPropGetter");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");
            prop4Getter.ThrowIfNull ("prop4Getter");

            if (IsFinished || IsDisposed) return this;

            DeepOnCollection (myFireCallbacksCallback, collectionPropGetter, prop1Getter, prop2Getter, prop3Getter, prop4Getter);

            return this;
        }

        private NotificationChain DeepOnCollection<T1, T2> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<ObservableCollection<T1>>> collectionPropGetter,
            Expression<Func<T1, T2>> prop1Getter)
            where T1 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            collectionPropGetter.ThrowIfNull ("collectionPropGetter");
            prop1Getter.ThrowIfNull ("prop1Getter");

            if (IsFinished || IsDisposed) return this;

            // notify when the collection object completely changes
            On (collectionPropGetter);

            // notify when the collection is modified (items added/removed)
            var mgr = ParentManager.CreateOrGetCollectionManager (collectionPropGetter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .On (CollectionNotificationChainManager.ObservedCollectionPropertyName)
               .On (prop1Getter)
               .AndCall (topLevelCallback);

            return this;
        }

        private NotificationChain DeepOnCollection<T1, T2, T3> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<ObservableCollection<T1>>> collectionPropGetter,
            Expression<Func<T1, T2>> prop1Getter,
            Expression<Func<T2, T3>> prop2Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            collectionPropGetter.ThrowIfNull ("collectionPropGetter");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");

            if (IsFinished || IsDisposed) return this;

            On (collectionPropGetter);

            var mgr = ParentManager.CreateOrGetCollectionManager (collectionPropGetter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .On (CollectionNotificationChainManager.ObservedCollectionPropertyName)
               .DeepOn (topLevelCallback, prop1Getter, prop2Getter);

            return this;
        }

        private NotificationChain DeepOnCollection<T1, T2, T3, T4> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<ObservableCollection<T1>>> collectionPropGetter,
            Expression<Func<T1, T2>> prop1Getter,
            Expression<Func<T2, T3>> prop2Getter,
            Expression<Func<T3, T4>> prop3Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
            where T3 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            collectionPropGetter.ThrowIfNull ("collectionPropGetter");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");

            if (IsFinished || IsDisposed) return this;

            On (collectionPropGetter);

            var mgr = ParentManager.CreateOrGetCollectionManager (collectionPropGetter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .On (CollectionNotificationChainManager.ObservedCollectionPropertyName)
               .DeepOn (topLevelCallback, prop1Getter, prop2Getter, prop3Getter);

            return this;
        
        }
        private NotificationChain DeepOnCollection<T1, T2, T3, T4, T5> (
            NotificationChainCallback topLevelCallback,
            Expression<Func<ObservableCollection<T1>>> collectionPropGetter,
            Expression<Func<T1, T2>> prop1Getter,
            Expression<Func<T2, T3>> prop2Getter,
            Expression<Func<T3, T4>> prop3Getter,
            Expression<Func<T4, T5>> prop4Getter)
            where T1 : class, INotifyPropertyChanged
            where T2 : class, INotifyPropertyChanged
            where T3 : class, INotifyPropertyChanged
            where T4 : class, INotifyPropertyChanged
        {
            topLevelCallback.ThrowIfNull ("topLevelCallback");
            collectionPropGetter.ThrowIfNull ("collectionPropGetter");
            prop1Getter.ThrowIfNull ("prop1Getter");
            prop2Getter.ThrowIfNull ("prop2Getter");
            prop3Getter.ThrowIfNull ("prop3Getter");
            prop4Getter.ThrowIfNull ("prop4Getter");

            if (IsFinished || IsDisposed) return this;

            On (collectionPropGetter);

            var mgr = ParentManager.CreateOrGetCollectionManager (collectionPropGetter);

            mgr.CreateOrGet ("../" + DependentPropertyName)
               .On (CollectionNotificationChainManager.ObservedCollectionPropertyName)
               .DeepOn (topLevelCallback, prop1Getter, prop2Getter, prop3Getter, prop4Getter);

            return this;
        }

        /// <summary>
        /// Specifies an action to invoke when a notifying property is changed. Multiple actions can be invoked.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public NotificationChain AndCall (Action callback)
        {
            callback.ThrowIfNull ("callback");

            if (IsFinished || IsDisposed) return this;

            AndCall ((sender, notifyingProperty, dependentProperty) => callback ());

            return this;
        }

        /// <summary>
        /// Specifies an action to invoke when a notifying property is changed. Multiple actions can be invoked.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public NotificationChain AndCall (NotificationChainCallback callback)
        {
            callback.ThrowIfNull ("callback");

            if (IsFinished || IsDisposed) return this;

            if (myCallbacks.Contains (callback)) return this;

            myCallbacks.Add (callback);

            return this;
        }

        /// <summary>
        /// Removes all callbacks
        /// </summary>
        /// <returns></returns>
        public NotificationChain AndClearCalls ()
        {
            if (IsFinished) return this;

            myCallbacks.Clear ();

            return this;
        }

        /// <summary>
        /// Indicates that the chain has been fully defined and prevents further configuration.
        /// </summary>
        /// <param name="executeImmediately"></param>
        public void Finish (bool executeImmediately = false)
        {
            if (IsFinished) return;

            IsFinished = true;

            if (executeImmediately)
                Execute (null, null);
        }

        /// <summary>
        /// Indicates that the chain has been fully defined and prevents further configuration,
        /// also immediately executes all calls with the given sender and propertyName
        /// </summary>
        public void Finish (Object sender, String propertyName)
        {
            if (IsFinished) return;

            IsFinished = true;

            Execute (sender, propertyName);
        }

        /// <summary>
        /// Pushes PropertyChangedEventArgs input for processing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns>whether or not the callbacks were triggered</returns>
        public bool Publish (Object sender, PropertyChangedEventArgs args)
        {
            if (!myObservedPropertyNames.Contains (args.PropertyName))
            {
                if (!myObservedRegexes.Any (r => r.IsMatch (args.PropertyName))) return false;
            }

            Execute (sender, args.PropertyName);
            return true;
        }

        public void Execute ()
        {
            Execute (null, null);
        }

        public void Execute (Object sender, String notifyingProperty)
        {
            foreach (var c in myCallbacks.ToList ())
                c (sender, notifyingProperty, DependentPropertyName);
        }
    }
}