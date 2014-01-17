﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Com.PhilChuang.Utils.MvvmCommandWirer;
using Demo.Utils;
using Microsoft.Practices.Prism.Commands;

namespace Demo
{
    public class MainWindowViewModel : NotifyPropertyChangedBase
    {
        // ----- the old way of doing chained properties ------------------------------------------
        // ----- note that the "parent" properties (Int1 and Int2) have to know about its dependent property (IntSum)

        private int myExample1Int1;
        public int Example1Int1
        {
            get { return myExample1Int1; }
            set
            {
                myExample1Int1 = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => Example1IntSum);
            }
        }

        private int myExample1Int2;
        public int Example1Int2
        {
            get { return myExample1Int2; }
            set
            {
                myExample1Int2 = value;
                RaisePropertyChanged();
                RaisePropertyChanged(() => Example1IntSum);
            }
        }

        public int Example1IntSum
        {
            get { return myExample1Int1 + myExample1Int2; }
        }

        // ----- the new way of doing chained properties ------------------------------------------
        // ----- note that the "parent" properties (Int1 and Int2) DO NOT know about its dependent property (IntSum)
        // ----- the dependent property (IntSum) is now responsible for the knowledge that it depends on Int1 and Int2

        private int myExample2Int1;
        public int Example2Int1
        {
            get { return myExample2Int1; }
            set
            {
                myExample2Int1 = value;
                RaisePropertyChanged();
            }
        }

        private int myExample2Int2;
        public int Example2Int2
        {
            get { return myExample2Int2; }
            set
            {
                myExample2Int2 = value;
                RaisePropertyChanged();
            }
        }

        public int Example2IntSum
        {
            get
            {
                myChainedNotifications.Create ()
                                      .Register (cn => cn.On (this, () => Example2Int1)
                                                         .On (this, () => Example2Int2)
                                                         .AndCall ((notifyingName, chainedName) => RaisePropertyChanged (chainedName)))
                                      .Finish ();

                return myExample2Int1 + myExample2Int2;
            }
        }
 
        // ----- the new way of doing chained properties ------------------------------------------
        // ----- this variation allows any PropertyChangedEventHandler to be chained (not just INotifyPropertyChanged.PropertyChanged)

        private int myExample3Int1;
        public int Example3Int1
        {
            get { return myExample3Int1; }
            set
            {
                myExample3Int1 = value;
                RaisePropertyChanged();
            }
        }

        private int myExample3Int2;
        public int Example3Int2
        {
            get { return myExample3Int2; }
            set
            {
                myExample3Int2 = value;
                RaisePropertyChanged();
            }
        }

        public int Example3IntSum
        {
            get
            {
                CreateChain ()
                    .Register (cn => cn.On (() => Example3Int1)
                                       .On (() => Example3Int2))
                    .Finish ();

                return myExample3Int1 + myExample3Int2;
            }
        }
 
        // ----- sub-property chaining ------------------------------------------
        // ----- demonstrates chaining on a Property's Property

        private static readonly String[] s_Example4CommandText = new[]
                                                                 {
                                                                     "Create Randomizer",
                                                                     "Randomize()",
                                                                     "Clear Randomizer",
                                                                 };
        private int m_Example4CommandTextIndex = 0;
        public String Example4CommandText
        { get { return s_Example4CommandText[m_Example4CommandTextIndex]; } }

        [CommandProperty(commandType: typeof (DelegateCommand))]
        public ICommand Example4Command
        { get; set; }

        [CommandExecuteMethod]
        private void ExecuteExample4 ()
        {
            if (m_Example4CommandTextIndex == 0)
                Example4Randomizer = new RandomIntGenerator();
            else if (m_Example4CommandTextIndex == 1)
                Example4Randomizer.Randomize();
            else if (m_Example4CommandTextIndex == 2)
                Example4Randomizer = null;

            m_Example4CommandTextIndex = (m_Example4CommandTextIndex + 1) % s_Example4CommandText.Length;
            RaisePropertyChanged (() => Example4CommandText);
        }

        private RandomIntGenerator myExample4Randomizer;
        public RandomIntGenerator Example4Randomizer
        {
            get { return myExample4Randomizer; }
            set
            {
                myExample4Randomizer = value;
                RaisePropertyChanged();
            }
        }

        public int Example4Int
        {
            get
            {
                // notify when Example4Randomizer and Example4Randomizer.Int changes
                CreateChain ()
                    .Register (cn => cn.On (() => Example4Randomizer,
                                            e4r => e4r.Int))
                    .Finish ();

                return Example4Randomizer != null ? Example4Randomizer.Int : -1;
            }
        }

        public MainWindowViewModel ()
        {
            CommandWirer.WireAll (this);
        }
    }

    public class RandomIntGenerator : NotifyPropertyChangedBase
    {
        private int myInt;
        public int Int
        {
            get { return myInt; }
            set
            {
                myInt = value;
                RaisePropertyChanged ();
            }
        }

        public RandomIntGenerator ()
        {
            Randomize ();
        }

        public void Randomize ()
        {
            Int = new Random ().Next ();
        }
    }
}