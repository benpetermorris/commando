using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace twomindseye.Commando.Util
{
    public static class Evaluator
    {
        class EvaluatorInfo
        {
            public Expression RootExpression;
            public List<ParameterExpression> Parameters;
        }

        static readonly Dictionary<string, EvaluatorInfo> s_evaluators = new Dictionary<string, EvaluatorInfo>();

        static public string[] GetArgsUsed(string expr)
        {
            return s_tokenRegex.Matches(expr)
                .Cast<Match>()
                .Where(m => m.Groups["arg"].Success)
                .Select(m => m.Groups["arg"].Value.Substring(1))
                .Distinct()
                .ToArray();
        }

        static public void Validate(string expr, string[] argsMustBePresent, bool onlyTheseArgs)
        {
            var variableExpressions = new List<ParameterExpression>();
            CreateExpr(variableExpressions, s_tokenRegex.Matches(expr).Cast<Match>().ToArray());
            var args = GetArgsUsed(expr);

            if (argsMustBePresent.Except(args).Count() > 0)
            {
                throw new ArgumentException("Expression does not contain required arguments", "expr");
            }

            if (onlyTheseArgs && args.Except(argsMustBePresent).Count() > 0)
            {
                throw new ArgumentException("Expression uses arguments other than the specified", "expr");
            }
        }

        static public T CreateEvalFunc<T>(string expr, params string[] argNames)
        {
            var evaluatorInfo = GetEvaluatorInfo(expr, argNames);
            var lambda = Expression.Lambda<T>(evaluatorInfo.RootExpression, evaluatorInfo.Parameters);
            return lambda.Compile();
        }

        static EvaluatorInfo GetEvaluatorInfo(string expr, IEnumerable<string> argNames)
        {
            EvaluatorInfo evaluatorInfo;

            lock (s_evaluators)
            {
                if (!s_evaluators.TryGetValue(expr, out evaluatorInfo))
                {
                    evaluatorInfo = new EvaluatorInfo();
                    evaluatorInfo.Parameters = new List<ParameterExpression>();
                    evaluatorInfo.RootExpression = CreateExpr(evaluatorInfo.Parameters, 
                        s_tokenRegex.Matches(expr).Cast<Match>().ToArray());
                    s_evaluators.Add(expr, evaluatorInfo);
                }
            }

            if (evaluatorInfo.Parameters.Select(p => p.Name).Except(argNames).Any())
            {
                throw new ArgumentException("Expression uses arguments other than the specified", "expr");
            }

            return evaluatorInfo;
        }

        delegate Expression GenerateExprDelegate(Expression[] args);

        abstract class Token
        {
        }

        class Number : Token
        {
            public Expression Expression { get; set; }
        }

        class Operator : Token
        {
            public string Text { get; set; }
            public int ParamCount { get; set; }
            public GenerateExprDelegate GenerateExpr { get; set; }
            public int Precedence { get; set; }
        }

        static Operator[] Operators = new[]
        {
            new Operator 
            {
                Text = "-",
                ParamCount = 2,
                GenerateExpr = args => Expression.Subtract(args[0], args[1]),
                Precedence = 1
            },
            new Operator 
            {
                Text = "+",
                ParamCount = 2,
                GenerateExpr = args => Expression.Add(args[0], args[1]),
                Precedence = 2
            },
            new Operator 
            {
                Text = "*",
                ParamCount = 2,
                GenerateExpr = args => Expression.Multiply(args[0], args[1]),
                Precedence = 3
            },
            new Operator 
            {
                Text = "/",
                ParamCount = 2,
                GenerateExpr = args => Expression.Divide(args[0], args[1]),
                Precedence = 4
            },
            new Operator 
            {
                Text = "%",
                ParamCount = 2,
                GenerateExpr = args => Expression.Modulo(args[0], args[1]),
                Precedence = 4
            },
            new Operator
            {
                Text = "(",
                ParamCount = 1,
                Precedence = 100
            },
            new Operator 
            {
                Text = ")",
                ParamCount = 1,
                Precedence = 100
            },
            new Operator 
            {
                Text = "ABS",
                ParamCount = 1,
                GenerateExpr = args => Expression.Call(null, typeof(Math).GetMethod("Abs"), args[0]),
                Precedence = 10
            },
            new Operator
            {
                Text = "POW",
                ParamCount = 2,
                GenerateExpr = args => Expression.Call(null, typeof(Math).GetMethod("Pow"), args[0], args[1]),
                Precedence = 10
            }
        };

        private static int s_maxOperArgs = Operators.Select(o => o.ParamCount).Max();

        static Expression CreateExpr(List<ParameterExpression> variableExpressions, Match[] tokens)
        {
            var operands = new List<Token>();
            var operators = new Stack<Operator>();

            // convert to reverse polish notation

            foreach (var t in tokens)
            {
                var text = t.Groups[0].Value;

                if (t.Groups["num"].Success)
                {
                    var num = Expression.Constant(double.Parse(text));
                    operands.Add(new Number { Expression = num });
                }
                else if (t.Groups["op"].Success)
                {
                    var op = Operators.Single(o => o.Text == text);

                    if (op.Text == ")")
                    {
                        while (true)
                        {
                            if (operators.Count == 0)
                            {
                                throw new ArgumentException("Mismatched parentheses", "expr");
                            }

                            if (operators.Peek().Text == "(")
                            {
                                break;
                            }

                            operands.Add(operators.Pop());
                        }

                        operators.Pop();    // "("
                    }
                    else if (operators.Count == 0 ||
                             operators.Peek().Text == "(" ||
                             operators.Peek().Precedence < op.Precedence)
                    {
                        operators.Push(op);
                    }
                    else
                    {
                        operands.Add(operators.Pop());
                        operators.Push(op);
                    }
                }
                else if (t.Groups["arg"].Success)
                {
                    var argName = text.Substring(1);
                    var varExpr = variableExpressions.FirstOrDefault(ve => ve.Name == argName);

                    if (varExpr == null)
                    {
                        varExpr = Expression.Parameter(typeof(double), argName);
                        variableExpressions.Add(varExpr);
                    }

                    operands.Add(new Number { Expression = varExpr });
                }
            }

            while (operators.Count > 0)
            {
                operands.Add(operators.Pop());
            }

            var args = new List<Expression>();
            var operArgs = new Expression[s_maxOperArgs];

            foreach (var op in operands)
            {
                if (op is Number)
                {
                    args.Add((op as Number).Expression);
                }
                else
                {
                    var oper = op as Operator;

                    if (args.Count < oper.ParamCount)
                    {
                        throw new ArgumentException("Insufficient parameters for operator", "expr");
                    }

                    for (int di = 0, i = args.Count - oper.ParamCount; i < args.Count; di++)
                    {
                        operArgs[di] = args[i];
                        args.RemoveAt(i);
                    }

                    args.Add(oper.GenerateExpr(operArgs));
                }
            }

            if (args.Count > 1)
            {
                throw new ArgumentException("Extra tokens in expression", "expr");
            }

            return args[0];
        }

        static Regex s_tokenRegex = new Regex(@"(?'num'[\+\-]?(\d+\.)?\d+)|(?'op'[\(\)\-\+\*\%\/]|ABS|POW)|(?'arg'\@[A-Za-z0-9_]+)", RegexOptions.IgnoreCase);
    }
}
