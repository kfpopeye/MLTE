using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MLTE
{
	public class ParameterItems : INotifyPropertyChanged
	{
        /// <summary>
        /// For use with design time
        /// </summary>
        public ParameterItems()
        {
        }

        /// <summary>
        /// Used with TextEditorWindow
        /// </summary>
        public ParameterItems(int pid, string _name, string _group, string _thevalue, string ptype, bool is_tokenized)
        {
            _parameterId = pid;
            P_Type = ptype;
            ItsValueType = is_tokenized ? "knockknock" : "text";
            Name = _name;
            Group = _group;
            _value = _thevalue;
        }

        /// <summary>
        /// Used by Formula Editor
        /// </summary>
        public ParameterItems(int pid, string _name, string _formula, string _group, string _thevalue, string ptype)
        {
            _famParameterId = pid;
            Formula = _formula;
            P_Type = ptype;
            Name = _name;
            Group = _group;
            _value = _thevalue;
        }

        public System.Collections.Generic.IList<string> TokenList { get { return _tokenList; } set { _tokenList = value; } }

        public int ParameterId { get{return _parameterId;} }

        public int Fam_ParameterId { get { return _famParameterId; } }

        public string Name { get; set; }

		public string Value {
			get{ return this._value; }
			set
			{
				if(this._value != value)
				{
					this._value = value;
					this.NotifyPropertyChanged("Value");
				}
			}
		}

		public string Formula {
			get{ return this._formula; }
			set
			{
				if(this._formula != value)
				{
					this._formula = value;
					this.NotifyPropertyChanged("Formula");
				}
			}
		}

        public string Group { get; set; }

		public string P_Type{ get; set; } // used by text editor to store "type" or "instance"
        public string ItsValueType { get; set; }
		
		public event PropertyChangedEventHandler PropertyChanged;
        private int _parameterId = -1;
        private int _famParameterId = -1;
		private string _formula = null;
		private string _value = null;
        private System.Collections.Generic.IList<string> _tokenList = null;

		public void NotifyPropertyChanged(string propName)
		{
			if(this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}
}
