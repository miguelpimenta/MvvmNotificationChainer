﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using Com.PhilChuang.Utils;
using Com.PhilChuang.Utils.MvvmNotificationChainer;

namespace Demo.Utils
{
    public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged, IDisposable
    {
        public virtual event PropertyChangedEventHandler PropertyChanged = delegate { }; 
        protected virtual event PropertyChangedEventHandler PropertyChangedInternal = delegate { };

        protected virtual void RaisePropertyChanged<T> (Expression<Func<T>> propertyExpression)
        {
            RaisePropertyChanged (propertyExpression.GetPropertyName ());
        }

        protected virtual void RaisePropertyChanged ([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged (new PropertyChangedEventArgs (propertyName));
        }

        protected virtual void RaisePropertyChanged (PropertyChangedEventArgs args)
        {
            if (PropertyChanged != null)
            {
                var handler = PropertyChanged;
                handler (this, args);
            }
            RaisePropertyChangedInternal (args);
        }

        protected virtual void RaisePropertyChangedInternal ([CallerMemberName] string propertyName = null)
        {
            RaisePropertyChangedInternal (new PropertyChangedEventArgs (propertyName));
        }

        protected virtual void RaisePropertyChangedInternal (PropertyChangedEventArgs args)
        {
            if (PropertyChangedInternal != null)
            {
                var handler = PropertyChangedInternal;
                handler (this, args);
            }
        }

        protected readonly NotificationChainManager myNotificationChainManager = new NotificationChainManager ();

        protected NotifyPropertyChangedBase ()
        {
            myNotificationChainManager.AddDefaultCall ((sender, notifyingProperty, dependentProperty) => RaisePropertyChanged (dependentProperty));
            myNotificationChainManager.Observe (this, h => PropertyChangedInternal += h, h => PropertyChangedInternal -= h);
        }

        public virtual void Dispose ()
        {
            myNotificationChainManager.Dispose ();
        }
    }

    public abstract class NotifyPropertyChangedBaseDebug : NotifyPropertyChangedBase
    {
        private event PropertyChangedEventHandler _PropertyChanged = delegate { }; 
        public override event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                lock (this)
                {
                    _PropertyChanged += value;
                    AppendPropertyChangedOutput("[PC] += handler on {0} from {1}".FormatWith(GetType().Name, value.Method.DeclaringType.Name));
                }
            }
            remove
            {
                lock (this)
                {
                    _PropertyChanged -= value;
                    AppendPropertyChangedOutput("[PC] -= handler on {0} from {1}".FormatWith(GetType().Name, value.Method.DeclaringType.Name));
                }
            }
        }

        private event PropertyChangedEventHandler _PropertyChangedInternal = delegate { };
        protected override event PropertyChangedEventHandler PropertyChangedInternal
        {
            add
            {
                lock (this)
                {
                    _PropertyChangedInternal += value;
                    AppendPropertyChangedOutput("[PCi] += handler on {0} from {1}".FormatWith(GetType().Name, value.Method.DeclaringType.Name));
                }
            }
            remove
            {
                lock (this)
                {
                    _PropertyChangedInternal -= value;
                    AppendPropertyChangedOutput("[PCi] -= handler on {0} from {1}".FormatWith(GetType().Name, value.Method.DeclaringType.Name));
                }
            }
        }

        protected override void RaisePropertyChanged (PropertyChangedEventArgs args)
        {
            if (_PropertyChanged != null)
            {
                if (args.PropertyName != "PropertyChangedOutput")
                    AppendPropertyChangedOutput ("[PC] " + args.PropertyName);
                var handler = _PropertyChanged;
                handler (this, args);
            }
            RaisePropertyChangedInternal (args);
        }

        protected override void RaisePropertyChangedInternal (PropertyChangedEventArgs args)
        {
            if (_PropertyChangedInternal != null)
            {
                if (args.PropertyName != "PropertyChangedOutput")
                    AppendPropertyChangedOutput ("[PCi] " + args.PropertyName);
                var handler = _PropertyChangedInternal;
                handler (this, args);
            }
        }

        private readonly StringBuilder myPropertyChangedOutput = new StringBuilder ();
        private void AppendPropertyChangedOutput (String line)
        {
            myPropertyChangedOutput.AppendFormat ("[{0:s}] {1}\r\n", DateTime.Now, line);
            RaisePropertyChanged (() => PropertyChangedOutput);
        }

        public String PropertyChangedOutput
        { get { return myPropertyChangedOutput.ToString (); } }
    }
}