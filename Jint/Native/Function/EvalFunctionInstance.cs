﻿using Esprima;
using Jint.Runtime;
using Jint.Runtime.Environments;

namespace Jint.Native.Function
{
    public class EvalFunctionInstance: FunctionInstance
    {
        private readonly Engine _engine;

        public EvalFunctionInstance(Engine engine, string[] parameters, LexicalEnvironment scope, bool strict) : base(engine, parameters, scope, strict)
        {
            _engine = engine;
            Prototype = Engine.Function.PrototypeObject;
            FastAddProperty("length", 1, false, false, false);
        }

        public override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            return Call(thisObject, arguments, false);
        }

        public JsValue Call(JsValue thisObject, JsValue[] arguments, bool directCall)
        {
            if (arguments.At(0).Type != Types.String)
            {
                return arguments.At(0);
            }

            var code = TypeConverter.ToString(arguments.At(0));

            try
            {
                var parser = new JavaScriptParser(code, new ParserOptions { AdaptRegexp = true, Tolerant = false });
                var program = parser.ParseProgram(StrictModeScope.IsStrictModeCode);
                using (new StrictModeScope(program.Strict))
                {
                    using (new EvalCodeScope())
                    {
                        LexicalEnvironment strictVarEnv = null;

                        try
                        {
                            if (!directCall)
                            {
                                Engine.EnterExecutionContext(Engine.GlobalEnvironment, Engine.GlobalEnvironment, Engine.Global);
                            }

                            if (StrictModeScope.IsStrictModeCode)
                            {
                                strictVarEnv = LexicalEnvironment.NewDeclarativeEnvironment(Engine, Engine.ExecutionContext.LexicalEnvironment);
                                Engine.EnterExecutionContext(strictVarEnv, strictVarEnv, Engine.ExecutionContext.ThisBinding);
                            }

                            Engine.DeclarationBindingInstantiation(DeclarationBindingType.EvalCode, program.HoistingScope.FunctionDeclarations, program.HoistingScope.VariableDeclarations, this, arguments);

                            var result = _engine.ExecuteStatement(program);

                            if (result.Type == Completion.Throw)
                            {
                                throw new JavaScriptException(result.GetValueOrDefault())
                                    .SetCallstack(_engine, result.Location);
                            }
                            else
                            {
                                return result.GetValueOrDefault();
                            }
                        }
                        finally
                        {
                            if (strictVarEnv != null)
                            {
                                Engine.LeaveExecutionContext();
                            }

                            if (!directCall)
                            {
                                Engine.LeaveExecutionContext();
                            }
                        }
                    }
                }
            }
            catch (ParserException e)
            {
                if (e.Description == Messages.InvalidLHSInAssignment)
                {
                    throw new JavaScriptException(Engine.ReferenceError);
                }

                throw new JavaScriptException(Engine.SyntaxError);
            }
        }
    }
}
