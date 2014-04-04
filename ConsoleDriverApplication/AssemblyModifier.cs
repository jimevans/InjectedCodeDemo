// <copyright file="AssemblyModifier.cs" company="Jim Evans">
//
// Copyright 2014 Jim Evans
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ConsoleDriverApplication
{
    /// <summary>
    /// Class that modifies assembly to inject code.
    /// </summary>
    public class AssemblyModifier
    {
        /// <summary>
        /// Modifies an assembly.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly to modify.</param>
        /// <param name="className">Name of the class to inject code into.</param>
        /// <param name="methodName">Name of the method to inject code into.</param>
        public static void ModifyAssembly(string assemblyName, string className, string methodName)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(assemblyName);
            string backupFileName = fileNameWithoutExtension + ".backup.dll";
            File.Copy(assemblyName, backupFileName, true);

            AssemblyDefinition definition = AssemblyDefinition.ReadAssembly("InjectedCode.dll");
            var typedefs = definition.MainModule.GetTypes();
            TypeDefinition typedef = typedefs.Where(d => d.FullName == "InjectedCode.CommandDispatcher").First();
            TypeReference typeref = definition.MainModule.Import(typedef);
            TypeReference voidType = definition.MainModule.Import(typeof(void));
            MethodDefinition instanceMethodDefinition = typedef.Methods.Where(m => m.Name == "get_Instance").First();
            MethodDefinition actualMethodDefinition = typedef.Methods.Where(m => m.Name == "Start").First();

            MethodDefinition targetMethod = null;
            AssemblyDefinition targetAssembly = AssemblyDefinition.ReadAssembly(assemblyName);
            TypeDefinition targetType = targetAssembly.MainModule.GetType(className);
            foreach (MethodDefinition method in targetType.Methods)
            {
                if (method.Name == methodName)
                {
                    targetMethod = method;
                    break;
                }
            }

            if (targetMethod != null)
            {
                TypeReference importedTypeRef = targetAssembly.MainModule.Import(typeref);
                MethodReference instanceMethodReference = targetAssembly.MainModule.Import(instanceMethodDefinition, importedTypeRef);
                MethodReference actualMethodReference = targetAssembly.MainModule.Import(actualMethodDefinition, importedTypeRef);

                ILProcessor processor = targetMethod.Body.GetILProcessor();
                Instruction ins = targetMethod.Body.Instructions.Last();
                Instruction call = processor.Create(Mono.Cecil.Cil.OpCodes.Call, instanceMethodReference);
                Instruction callvirt = processor.Create(Mono.Cecil.Cil.OpCodes.Callvirt, actualMethodReference);

                processor.InsertBefore(ins, call);
                processor.InsertBefore(ins, callvirt);
                targetAssembly.Write(assemblyName);
            }

            var pdbFileName = string.Format("{0}.pdb", fileNameWithoutExtension);
            var pdbExists = File.Exists(pdbFileName);
        }
    }
}
