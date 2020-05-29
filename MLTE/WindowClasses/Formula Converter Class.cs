using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;
using System.Text.RegularExpressions;

namespace WindowClasses
{
    public class FormulaConverter : IValueConverter
    {
        //convert to single line
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string s = value as string;
            s = s.Replace("\t", "");
            return s.Replace(Environment.NewLine, " ");
        }

        //for regex testing
        //Test#1    IF(Length < 35' , 2' 6" , IF(Length < 45' , 3' , IF(Length < 55' , 5' , 8' )))
        //Test#1    IF(Length < 35' , IF(Length < 45' , 3' , IF(Length < 55' , 5' , 8' )) , 2' 6")
        //Test#2    if((Width + 1') > 35', 36', 45')
        //Test#3    IF ( OR ( A = 1 , B = 3 ) , 8 , 3 ) 
        //test#4    IF ( AND (x = 1 , y = 2), 8 , 3 )

        //convert to multi-line
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string s = value as string;
            if (s == null)
                return string.Empty;

            Regex if_reg = new Regex(@"^if\s*\(", RegexOptions.IgnoreCase);
            if (!if_reg.IsMatch(s))
                return s;

            return recursive_parse(s, 0);
        }

        private string recursive_parse(string s, int IndentCounter)
        {
            StringBuilder sb = new StringBuilder();
            Regex reg = new Regex(@"
                (?# find first comma from start of sentance)
                (^[^,]*,)
                (?# Group the rest of the expression for further parsing)
                (?<inner>.*)
                ", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            MatchCollection mc = reg.Matches(s);
            sb.Append(getTabs(IndentCounter));
                
            if (mc.Count != 0)
            {
                if (IsIfExpression(mc[0].Groups[1].Value))
                    IndentCounter++;
                if (HasAndOrNotExpression(mc[0].Groups[1].Value))
                {
                    sb.Append(mc[0].Groups[1].Value);
                    string exp = GetNextExpression(mc[0].Groups["inner"].Value);
                    sb.AppendLine(exp.Trim());
                    sb.Append(recursive_parse(mc[0].Groups["inner"].Value.Remove(0, exp.Length).Trim(), IndentCounter));
                }
                else
                {
                    sb.AppendLine(mc[0].Groups[1].Value);
                    if (!IsIfExpression(mc[0].Groups[1].Value))
                    {
                        //if has right parenthesis --IndentCounter
                        foreach (char c in mc[0].Groups[1].Value.ToCharArray())
                        {
                            if (c == ')')
                                --IndentCounter;
                        }
                    }
                    sb.Append(recursive_parse(mc[0].Groups["inner"].Value.Trim(), IndentCounter));
                }
            }
            else
                sb.Append(s);

            return sb.ToString();
        }

        private string GetNextExpression(string s)
        {
            StringBuilder sb = new StringBuilder();

            Regex reg = new Regex(@"
                (?# find first comma from start of sentance)
                (^[^,]*,)
                (?# Group the rest of the expression for further parsing)
                (?<inner>.*)
                ", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            MatchCollection mc = reg.Matches(s);
            if (mc.Count != 0)
            {
                    return mc[0].Groups[1].Value;
            }
            throw new Exception("Next expression not found");
        }

        private bool HasAndOrNotExpression(string s)
        {
            Regex reg = new Regex(@"
                (?#Match AND or OR or NOT and both parenthesis)
                ((and|or|not)\s*\()
                ", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            MatchCollection mc = reg.Matches(s);
            if (mc.Count != 0)
                return true;
            return false;
        }

        private bool IsIfExpression(string s)
        {
            Regex reg = new Regex(@"
                (?# Match IF at the beginning)
                (^if\s*\()
                ", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            MatchCollection mc = reg.Matches(s);
            if (mc.Count != 0)
                return true;
            return false;
        }

        private string getTabs(int x)
        {
            if (x <= 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < x; ++y)
            {
                sb.Append("\t");
            }

            return sb.ToString();
        }
    }
}

//        private ExpressionType GetExpressionType(string s)
//        {
//            //find IF
//            Regex reg = new Regex(@"
//                (?# Match IF at the beginning)
//                (^if\s*\()
//                ", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
//            MatchCollection mc = reg.Matches(s);
//            if (mc.Count != 0)
//                return ExpressionType.If_type;

//            //find AND\OR\NOT statement
//            reg = new Regex(@"
//                (?#Match AND or OR or NOT and both parenthesis)
//                ((and|or|not)\s*\()
//                ", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
//            mc = reg.Matches(s);
//            if (mc.Count != 0)
//                return ExpressionType.AndOrNot_type;

//            //find condition statement
//            reg = new Regex(@"
//                (?#Match condition)
//                (^[^<>=,]*[<>=][^,]*,)
//                ", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
//            mc = reg.Matches(s);
//            if (mc.Count != 0)
//                return ExpressionType.Condition_type;

//            //find EVALUATION or EXPRESSION with EVALUATION
//            reg = new Regex(@"
//                (?#Match evaluation)
//                (^[^,]*,)
//                ", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
//            mc = reg.Matches(s);
//            if (mc.Count != 0)
//                return ExpressionType.Eval_type;

//            //find last value before closing brackets
//            return ExpressionType.Value_type;
//        }
