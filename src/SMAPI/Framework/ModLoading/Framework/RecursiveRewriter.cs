using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace StardewModdingAPI.Framework.ModLoading.Framework;

/// <summary>Handles recursively rewriting loaded assembly code.</summary>
[SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Rewrite callbacks are invoked immediately.")]
internal class RecursiveRewriter
{
    /*********
    ** Delegates
    *********/
    /// <summary>Rewrite a module definition in the assembly code.</summary>
    /// <param name="module">The current module definition.</param>
    /// <returns>Returns whether the module was changed.</returns>
    public delegate bool RewriteModuleDelegate(ModuleDefinition module);

    /// <summary>Rewrite a type reference in the assembly code.</summary>
    /// <param name="type">The current type reference.</param>
    /// <param name="replaceWith">Replaces the type reference with the given type.</param>
    /// <returns>Returns whether the type was changed.</returns>
    public delegate bool RewriteTypeDelegate(TypeReference type, Action<TypeReference> replaceWith);

    /// <summary>Rewrite a CIL instruction in the assembly code.</summary>
    /// <param name="instruction">The current CIL instruction.</param>
    /// <param name="cil">The CIL instruction processor.</param>
    /// <returns>Returns whether the instruction was changed.</returns>
    public delegate bool RewriteInstructionDelegate(ref Instruction instruction, ILProcessor cil);


    /*********
    ** Accessors
    *********/
    /// <summary>The module to rewrite.</summary>
    public ModuleDefinition Module { get; }

    /// <summary>Handle or rewrite a module definition if needed.</summary>
    public RewriteModuleDelegate RewriteModuleImpl { get; }

    /// <summary>Handle or rewrite a type reference if needed.</summary>
    public RewriteTypeDelegate RewriteTypeImpl { get; }

    /// <summary>Handle or rewrite a CIL instruction if needed.</summary>
    public RewriteInstructionDelegate RewriteInstructionImpl { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="module">The module to rewrite.</param>
    /// <param name="rewriteModule">Handle or rewrite a module if needed.</param>
    /// <param name="rewriteType">Handle or rewrite a type reference if needed.</param>
    /// <param name="rewriteInstruction">Handle or rewrite a CIL instruction if needed.</param>
    public RecursiveRewriter(ModuleDefinition module, RewriteModuleDelegate rewriteModule, RewriteTypeDelegate rewriteType, RewriteInstructionDelegate rewriteInstruction)
    {
        this.Module = module;
        this.RewriteModuleImpl = rewriteModule;
        this.RewriteTypeImpl = rewriteType;
        this.RewriteInstructionImpl = rewriteInstruction;
    }

    /// <summary>Rewrite the loaded module code.</summary>
    /// <returns>Returns whether the module was modified.</returns>
    public bool RewriteModule()
    {
        IEnumerable<TypeDefinition> types = this.Module.GetTypes().Where(type => type.BaseType != null); // skip special types like <Module>

        bool changed = false;

        try
        {
            changed |= this.RewriteModuleImpl(this.Module);

            foreach (TypeDefinition type in types)
                changed |= this.RewriteTypeDefinition(type);
        }
        catch (Exception ex)
        {
            throw new Exception($"Rewriting {this.Module.Name} failed.", ex);
        }

        return changed;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Rewrite a loaded type definition.</summary>
    /// <param name="type">The type definition to rewrite.</param>
    /// <returns>Returns whether the type was modified.</returns>
    private bool RewriteTypeDefinition(TypeDefinition type)
    {
        bool changed = false;

        changed |= this.RewriteCustomAttributes(type.CustomAttributes);
        changed |= this.RewriteGenericParameters(type.GenericParameters);

        foreach (InterfaceImplementation @interface in type.Interfaces)
            changed |= this.RewriteTypeReference(@interface.InterfaceType, newType => @interface.InterfaceType = newType);

        if (type.BaseType.FullName != "System.Object")
            changed |= this.RewriteTypeReference(type.BaseType, newType => type.BaseType = newType);

        foreach (MethodDefinition method in type.Methods)
        {
            changed |= this.RewriteTypeReference(method.ReturnType, newType => method.ReturnType = newType);
            changed |= this.RewriteGenericParameters(method.GenericParameters);
            changed |= this.RewriteCustomAttributes(method.CustomAttributes);

            foreach (ParameterDefinition parameter in method.Parameters)
                changed |= this.RewriteTypeReference(parameter.ParameterType, newType => parameter.ParameterType = newType);

            foreach (var methodOverride in method.Overrides)
                changed |= this.RewriteMethodReference(methodOverride);

            if (method.HasBody)
            {
                foreach (VariableDefinition variable in method.Body.Variables)
                    changed |= this.RewriteTypeReference(variable.VariableType, newType => variable.VariableType = newType);

                // rewrite CIL instructions
                ILProcessor cil = method.Body.GetILProcessor();
                Collection<Instruction> instructions = cil.Body.Instructions;
                bool addedInstructions = false;
                // ReSharper disable once ForCanBeConvertedToForeach -- deliberate to allow changing the collection
                for (int i = 0; i < instructions.Count; i++)
                {
                    Instruction instruction = instructions[i];
                    if (instruction.OpCode.Code == Code.Nop)
                        continue;

                    int oldCount = cil.Body.Instructions.Count;
                    changed |= this.RewriteInstruction(instruction, cil, newInstruction =>
                    {
                        changed = true;
                        cil.Replace(instruction, newInstruction);
                        instruction = newInstruction;
                    });

                    if (cil.Body.Instructions.Count > oldCount)
                        addedInstructions = true;
                }

                // special case: added instructions may cause an instruction to be out of range
                // of a short jump that references it
                if (addedInstructions)
                {
                    foreach (var instruction in instructions)
                    {
                        var longJumpCode = RewriteHelper.GetEquivalentLongJumpCode(instruction.OpCode);
                        if (longJumpCode != null)
                            instruction.OpCode = longJumpCode.Value;
                    }
                    changed = true;
                }
            }
        }

        return changed;
    }

    /// <summary>Rewrite a CIL instruction if needed.</summary>
    /// <param name="instruction">The current CIL instruction.</param>
    /// <param name="cil">The CIL instruction processor.</param>
    /// <param name="replaceWith">Replaces the CIL instruction with a new one.</param>
    private bool RewriteInstruction(Instruction instruction, ILProcessor cil, Action<Instruction> replaceWith)
    {
        bool rewritten = false;

        // field reference
        FieldReference? fieldRef = RewriteHelper.AsFieldReference(instruction);
        if (fieldRef != null)
        {
            rewritten |= this.RewriteTypeReference(fieldRef.DeclaringType, newType => fieldRef.DeclaringType = newType);
            rewritten |= this.RewriteTypeReference(fieldRef.FieldType, newType => fieldRef.FieldType = newType);
        }

        // method reference
        MethodReference? methodRef = RewriteHelper.AsMethodReference(instruction);
        if (methodRef != null)
            this.RewriteMethodReference(methodRef);

        // type reference
        if (instruction.Operand is TypeReference typeRef)
            rewritten |= this.RewriteTypeReference(typeRef, newType => replaceWith(cil.Create(instruction.OpCode, newType)));

        // instruction itself
        // (should be done after the above type rewrites to ensure valid types)
        rewritten |= this.RewriteInstructionImpl(ref instruction, cil);

        return rewritten;
    }

    /// <summary>Rewrite a method reference if needed.</summary>
    /// <param name="methodRef">The current method reference.</param>
    private bool RewriteMethodReference(MethodReference methodRef)
    {
        bool rewritten = false;

        rewritten |= this.RewriteTypeReference(methodRef.DeclaringType, newType =>
        {
            // note: generic methods are wrapped into a MethodSpecification which doesn't allow changing the
            // declaring type directly. For our purposes we want to change all generic versions of a matched
            // method anyway, so we can use GetElementMethod to get the underlying method here.
            methodRef.GetElementMethod().DeclaringType = newType;
        });
        rewritten |= this.RewriteTypeReference(methodRef.ReturnType, newType => methodRef.ReturnType = newType);

        foreach (ParameterDefinition parameter in methodRef.Parameters)
            rewritten |= this.RewriteTypeReference(parameter.ParameterType, newType => parameter.ParameterType = newType);

        if (methodRef is GenericInstanceMethod genericRef)
        {
            for (int i = 0; i < genericRef.GenericArguments.Count; i++)
                rewritten |= this.RewriteTypeReference(genericRef.GenericArguments[i], newType => genericRef.GenericArguments[i] = newType);
        }

        return rewritten;
    }

    /// <summary>Rewrite a type reference if needed.</summary>
    /// <param name="type">The current type reference.</param>
    /// <param name="replaceWith">Replaces the type reference with a new one.</param>
    private bool RewriteTypeReference(TypeReference type, Action<TypeReference> replaceWith)
    {
        bool rewritten = false;

        // type
        rewritten |= this.RewriteTypeImpl(type, newType =>
        {
            type = newType;
            replaceWith(newType);
            rewritten = true;
        });

        // generic arguments
        if (type is GenericInstanceType genericType)
        {
            for (int i = 0; i < genericType.GenericArguments.Count; i++)
                rewritten |= this.RewriteTypeReference(genericType.GenericArguments[i], typeRef => genericType.GenericArguments[i] = typeRef);
        }

        // generic parameters (e.g. constraints)
        rewritten |= this.RewriteGenericParameters(type.GenericParameters);

        return rewritten;
    }

    /// <summary>Rewrite custom attributes if needed.</summary>
    /// <param name="attributes">The current custom attributes.</param>
    private bool RewriteCustomAttributes(Collection<CustomAttribute> attributes)
    {
        bool rewritten = false;

        for (int attrIndex = 0; attrIndex < attributes.Count; attrIndex++)
        {
            CustomAttribute attribute = attributes[attrIndex];
            bool curChanged = false;

            // attribute type
            TypeReference? newAttrType = null;
            rewritten |= this.RewriteTypeReference(attribute.AttributeType, newType =>
            {
                newAttrType = newType;
                curChanged = true;
            });

            // constructor arguments
            TypeReference[] argTypes = new TypeReference[attribute.ConstructorArguments.Count];
            for (int i = 0; i < argTypes.Length; i++)
            {
                var arg = attribute.ConstructorArguments[i];

                argTypes[i] = arg.Type;
                rewritten |= this.RewriteTypeReference(arg.Type, newType =>
                {
                    argTypes[i] = newType;
                    curChanged = true;
                });
            }

            // swap attribute
            if (curChanged)
            {
                // get constructor
                MethodDefinition? constructor = (newAttrType ?? attribute.AttributeType)
                    .Resolve()
                    ?.Methods
                    .Where(method => method.IsConstructor)
                    .FirstOrDefault(ctor => RewriteHelper.HasMatchingSignature(ctor, attribute.Constructor));
                if (constructor == null)
                    throw new InvalidOperationException($"Can't rewrite attribute type '{attribute.AttributeType.FullName}' to '{newAttrType?.FullName}', no equivalent constructor found.");

                // create new attribute
                var newAttr = new CustomAttribute(this.Module.ImportReference(constructor));
                for (int i = 0; i < argTypes.Length; i++)
                    newAttr.ConstructorArguments.Add(new CustomAttributeArgument(argTypes[i], attribute.ConstructorArguments[i].Value));
                foreach (CustomAttributeNamedArgument prop in attribute.Properties)
                    newAttr.Properties.Add(new CustomAttributeNamedArgument(prop.Name, prop.Argument));
                foreach (CustomAttributeNamedArgument field in attribute.Fields)
                    newAttr.Fields.Add(new CustomAttributeNamedArgument(field.Name, field.Argument));

                // swap attribute
                attributes[attrIndex] = newAttr;
                rewritten = true;
            }
        }

        return rewritten;
    }

    /// <summary>Rewrites generic type parameters if needed.</summary>
    /// <param name="parameters">The current generic type parameters.</param>
    private bool RewriteGenericParameters(Collection<GenericParameter> parameters)
    {
        bool anyChanged = false;

        for (int i = 0; i < parameters.Count; i++)
        {
            TypeReference parameter = parameters[i];
            anyChanged |= this.RewriteTypeReference(parameter, newType => parameters[i] = new GenericParameter(parameter.Name, newType));
        }

        return anyChanged;
    }
}
