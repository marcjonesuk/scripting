using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;

namespace scriptlang
{
    public class State
    {
        public static Stack<object[]> Args = new Stack<object[]>();
        public static int StackDepth = 0;
        //public static Dictionary<string, object> Functions = new Dictionary<string, object>();

        public static List<Dictionary<string, object>> Functions = new List<Dictionary<string, object>>();

        public static Dictionary<string, object> Global = new Dictionary<string, object>();

        public static HashSet<string> Const = new HashSet<string>();

        static void AssertArgCount(object[] args, int count, string functionName)
        {
            if (args.Length != count)
            {
                throw new RuntimeException($"{functionName} should be invoked with {count} arg(s)");
            }
        }

        public static (object, bool) Resolve(string name)
        {
            for (var stack = Functions.Count - 1; stack >= 0; stack--)
            {
                if (Functions[stack].ContainsKey(name))
                    return (Functions[stack][name], true);
            }
            return (null, false);
        }

        public static object GetValue(List<string> parts)
        {
            var (result, found) = Resolve(parts[0]);
            if (!found)
                throw new RuntimeException($"Unknown symbol: {parts[0]}");
            if (parts.Count == 1)
                return result;

            var current = Resolve(parts[0]).Item1;
            for (var i = 1; i < parts.Count; i++)
            {
                var p = parts[i];
                current = GetObjectProperty(current, p);
            }
            return current;
        }

        private static object GetObjectProperty(object root, string property)
        {
            if (root is IDictionary<string, object> dict)
            {
                if (!dict.ContainsKey(property))
                    throw new RuntimeException("Member does not exist");
                return dict[property];
            }
            throw new RuntimeException("TODO: cant get property");
        }

        private static object SetObjectProperty(object obj, string property, object value)
        {
            if (obj is IDictionary<string, object> dict)
            {
                dict[property] = value;
                return value;
            }
            throw new RuntimeException("TODO: cant get property");
        }

        public static object SetValue(List<string> parts, ScriptFunction value)
        {
            if (parts.Count == 1)
                return ((CustomFunction)Global["set"]).Invoke(new object[] { parts[0], value });

			var (current, found) = Resolve(parts[0]);
            if (!found)
            {
                throw new RuntimeException($"symbol {parts[0]} has not been assigned");
            }
            
            for (var i = 1; i < parts.Count; i++)
            {
                var p = parts[i];
                if (i == parts.Count - 1)
                    return SetObjectProperty(current, p, value.Invoke());
                current = GetObjectProperty(current, p);
            }
            throw new RuntimeException("SetValue failed");
        }

        static State()
        {
            Functions.Add(Global);

            Const.Add("var");
            Global["var"] = new CustomFunction(args =>
            {
                var func = args[0] as ScriptFunction;

                if (func == null)
                    throw new RuntimeException();

                var variableName = func.SymbolName;

                if (args.Length > 1)
                {
                    var valueFunc = args[1] as ScriptFunction;
                    Functions[StackDepth - 1][variableName] = valueFunc.Invoke();
                }
                else
                {
                    Functions[StackDepth - 1][variableName] = null;
                }
                return Functions[StackDepth - 1][variableName];
            });

            Global["args"] = new CustomFunction(args =>
            {
                if (args.Length == 0)
                    return Args.Peek();

                var index = (int)Convert.ChangeType(args[0], typeof(int));
                return Args.Peek()[index];
            });

            Global["props"] = new CustomFunction(args =>
            {
                if (args[0] is IDictionary<string, object> d)
                {
                    return new List<string>(d.Keys);
                }
                throw new RuntimeException("props: unable to return props");
            });

            Const.Add("const");
            Global["const"] = new CustomFunction(args =>
            {
                var func = args[0] as ScriptFunction;

                if (func == null)
                    throw new RuntimeException();

                var variableName = func.SymbolName;

                if (args.Length > 1)
                {
                    var valueFunc = args[1] as ScriptFunction;
                    Global[variableName] = valueFunc.Invoke();
                    Const.Add(variableName);
                }
                else
                {
                    throw new RuntimeException($"const ({variableName}) must be declared with a value");
                }
                return Global[variableName];
            });

            Const.Add("set");
            Global["set"] = new CustomFunction(args =>
            {
                string varName;
                if (args[0] is string s)
                {
                    varName = s;
                }
                else
                {
                    var func = args[0] as ScriptFunction;
                    if (func == null)
                        throw new RuntimeException();
                    if (string.IsNullOrEmpty(func.SymbolName))
                    {
                        throw new RuntimeException("The first argument of the set function should be a valid symbol");
                    }
                    varName = func.SymbolName;
                }

                if (Const.Contains(varName))
                {
                    throw new RuntimeException($"Cannot assign to const variable {varName}");
                }
                var newValue = ((ScriptFunction)args[1]).Invoke();
                if (newValue is ScriptFunction sf)
                {
                    newValue = new CustomFunction(lambdaArgs =>
                    {
                        Args.Push(lambdaArgs);
                        StackDepth++;
						Functions.Add(new Dictionary<string, object>());
                        var result = sf.Invoke();
                        Args.Pop();
						Functions.RemoveAt(StackDepth);
                        StackDepth--;
                        return result;
                    });
                }

                Functions[StackDepth][varName] = newValue;
                return newValue;
            });

            Const.Add("throw");
            Global["throw"] = new CustomFunction(args =>
            {
                throw new Exception(args[0].ToString());
            });

            Global["write"] = new CustomFunction(args =>
            {
                Console.WriteLine(args[0].ToString());
                return null;
            });
            Global["new"] = new CustomFunction(args =>
            {
                if (args.Length == 0)
                    return new ExpandoObject();

                switch (args[0])
                {
                    case string s:
                        // todo: optimise this
                        var st = s.TrimStart();
                        if (st[0] == '[')
                            return JsonConvert.DeserializeObject<List<object>>(st);
                        else
                            return JsonConvert.DeserializeObject<ExpandoObject>(st);
                }

                throw new RuntimeException();
            });

            Global["add"] = new CustomFunction(args =>
            {
                return (dynamic)args[0] + (dynamic)args[1];
            });

            Global["len"] = new CustomFunction(args =>
            {
                if (args[0] is string s)
                    return s.Length;
                if (args[0] is IList l)
                    return l.Count;
                if (args[0] is ICollection c)
                    return c.Count;

                throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
            });

            Global["clear"] = new CustomFunction(args =>
            {
                if (args[0] is IList l)
                {
                    l.Clear();
                    return null;
                }
                throw new RuntimeException($"Cannot use len function on type {args[0].GetType()}");
            });

            Global["inc"] = new CustomFunction(args =>
            {
				// todo handle local state
                // AssertArgCount(args, 1, "inc");
                // var s = args[0] as ScriptFunction;
                // dynamic value = s.Invoke();
                // value++;
                // if (s.SymbolName != null)
                // {
                //     Global[s.SymbolName] = value;
                // }
                //return value;
				return null;
            });

            Global["dec"] = new CustomFunction(args =>
            {
                // AssertArgCount(args, 1, "inc");
                // var s = args[0] as ScriptFunction;
                // dynamic value = s.Invoke();
                // value--;
                // if (s.SymbolName != null)
                // {
                //     Global[s.SymbolName] = value;
                // }
                // return value;
				return null;
            });

            Global["eq"] = new CustomFunction(args =>
            {
                AssertArgCount(args, 2, "eq");
                var arg1 = args[0];

                // Comparing all other objects to the first.
                for (var i = 1; i < args.Length; i++)
                {
                    var ix = args[i];
                    if (arg1 == null && ix != null || arg1 != null && ix == null)
                        return false;
                    if (arg1 != null && !arg1.Equals(ix))
                        return false;
                }

                // Equality!
                return (object)true;
            });

            Const.Add("if");
            Global["if"] = new CustomFunction(args =>
            {
                if (Truthy(args[0]))
                {
                    if (args[1] is ScriptFunction s)
                    {
                        return s.Invoke();
                    }
                    else
                    {
                        return args[1];
                    }
                }
                else
                {
                    if (args.Length > 2)
                    {
                        if (args[2] is ScriptFunction s)
                        {
                            return s.Invoke();
                        }
                        else
                        {
                            return args[2];
                        }
                    }
                    return null;
                }
            });

            Global["json"] = new CustomFunction(args =>
            {
                return JsonConvert.SerializeObject(args[0]);
            });

            Const.Add("not");
            Global["not"] = new CustomFunction(args =>
            {
                return !Truthy(args[0]);
            });

            Const.Add("try");
            Global["try"] = new CustomFunction(args =>
            {
                if (args.Length == 0)
                {
                    throw new RuntimeException("try function does not have any try block");
                }

                try
                {
                    if (args[0] is ScriptFunction t)
                    {
                        return t.Invoke();
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    if (args.Length > 1)
                    {
                        if (args[1] is ScriptFunction c)
                        {
                            Args.Push(new object[] { ex.Message });
                            StackDepth++;
							Functions.Add(new Dictionary<string, object>());
                            var result = c.Invoke();
                            Args.Pop();
                            StackDepth--;
							Functions.RemoveAt(StackDepth - 1);
                            return result;
                        }
                    }
                    return ex;
                }
            });

            Const.Add("null");
            Global["null"] = null;
            Const.Add("true");
            Global["true"] = true;
            Const.Add("false");
            Global["false"] = false;
            Array.Do(Global);
        }

        static bool Truthy(object arg)
        {
            if (arg is bool b)
            {
                return b;
            }
            if (arg == null)
            {
                return false;
            }
            if (arg is string s)
            {
                return s != string.Empty;
            }
            return true;
        }
    }
}
