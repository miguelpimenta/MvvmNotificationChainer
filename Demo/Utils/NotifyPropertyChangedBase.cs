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
        private event PropertyChangedEventHandler _PropertyChanged;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                lock (this)
                {
                    PropertyChanged += value;
                    AppendPropertyChangedOutput("[PropertyChanged] += handler on {0}".FormatWith(GetType().Name));
                }
            }
            remove
            {
                lock (this)
                {
                    PropertyChanged -= value;
                    AppendPropertyChangedOutput("[PropertyChanged] -= handler on {0}".FormatWith(GetType().Name));
                }
            }
        }

        private event PropertyChangedEventHandler _PropertyChangedInternal;
        protected event PropertyChangedEventHandler PropertyChangedInternal
        {
            add
            {
                lock (this)
                {
                    _PropertyChangedInternal += value;
                    AppendPropertyChangedOutput ("[PropertyChangedInternal] += handler on {0}".FormatWith (GetType().Name));
                }
            }
            remove
            {
                lock (this)
                {
                    _PropertyChangedInternal -= value;
                    AppendPropertyChangedOutput("[PropertyChangedInternal] -= handler on {0}".FormatWith (GetType().Name));
                }
            }
        }

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
            if (_PropertyChanged != null)
            {
                if (args.PropertyName != "PropertyChangedOutput")
                    AppendPropertyChangedOutput ("[PropertyChanged] " + args.PropertyName);
                var handler = _PropertyChanged;
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
            if (_PropertyChangedInternal != null)
            {
                if (args.PropertyName != "PropertyChangedOutput")
                    AppendPropertyChangedOutput ("[PropertyChangedInternal] " + args.PropertyName);
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

        protected readonly NotificationChainManager myNotificationChainManager = new NotificationChainManager ();

        protected NotifyPropertyChangedBase ()
        {
            PropertyChanged += delegate { };
            PropertyChangedInternal += delegate { };

            myNotificationChainManager.Observe (this, h => PropertyChangedInternal += h, h => PropertyChangedInternal -= h);
            myNotificationChainManager.AddDefaultCall ((sender, notifyingProperty, dependentProperty) => RaisePropertyChanged (dependentProperty));
        }

        public virtual void Dispose ()
        {
            myNotificationChainManager.Dispose ();
        }
    }
}