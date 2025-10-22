using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using AdministraAoImoveis.Web.Controllers;
using AdministraAoImoveis.Web.Domain.Enumerations;
using AdministraAoImoveis.Web.Domain.Users;
using AdministraAoImoveis.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace AdministraAoImoveis.Web.Tests;

public class AuthorizationPolicyTests
{
    public static IEnumerable<object[]> ActionPolicyData()
    {
        yield return Policy(
            typeof(ContratosController),
            nameof(ContratosController.Index),
            PolicyNames.ContractsRead,
            typeof(bool),
            typeof(CancellationToken));
        yield return Policy(
            typeof(ContratosController),
            nameof(ContratosController.Create),
            PolicyNames.ContractsManage,
            typeof(Guid),
            typeof(CancellationToken));
        yield return Policy(
            typeof(ContratosController),
            nameof(ContratosController.Create),
            PolicyNames.ContractsManage,
            typeof(ContractGenerationInputModel),
            typeof(CancellationToken));
        yield return Policy(
            typeof(ContratosController),
            nameof(ContratosController.Details),
            PolicyNames.ContractsRead,
            typeof(Guid),
            typeof(CancellationToken));
        yield return Policy(
            typeof(ContratosController),
            nameof(ContratosController.Download),
            PolicyNames.ContractsRead,
            typeof(Guid),
            typeof(Guid),
            typeof(CancellationToken));
        yield return Policy(
            typeof(ContratosController),
            nameof(ContratosController.Ativar),
            PolicyNames.ContractsManage,
            typeof(Guid),
            typeof(CancellationToken));
        yield return Policy(
            typeof(ContratosController),
            nameof(ContratosController.Anexar),
            PolicyNames.ContractsManage,
            typeof(Guid),
            typeof(ContractAttachmentInputModel),
            typeof(CancellationToken));
        yield return Policy(
            typeof(ContratosController),
            nameof(ContratosController.Encerrar),
            PolicyNames.ContractsManage,
            typeof(Guid),
            typeof(ContractClosureInputModel),
            typeof(CancellationToken));

        yield return Policy(
            typeof(DocumentosController),
            nameof(DocumentosController.Index),
            PolicyNames.PropertyDocumentsRead,
            typeof(Guid),
            typeof(CancellationToken));
        yield return Policy(
            typeof(DocumentosController),
            nameof(DocumentosController.Upload),
            PolicyNames.PropertyDocumentsManage,
            typeof(Guid),
            typeof(PropertyDocumentUploadInputModel),
            typeof(CancellationToken));
        yield return Policy(
            typeof(DocumentosController),
            nameof(DocumentosController.Review),
            PolicyNames.PropertyDocumentsManage,
            typeof(Guid),
            typeof(Guid),
            typeof(PropertyDocumentReviewInputModel),
            typeof(CancellationToken));
        yield return Policy(
            typeof(DocumentosController),
            nameof(DocumentosController.RegistrarAceite),
            PolicyNames.PropertyDocumentsManage,
            typeof(Guid),
            typeof(Guid),
            typeof(PropertyDocumentAcceptanceInputModel),
            typeof(CancellationToken));
        yield return Policy(
            typeof(DocumentosController),
            nameof(DocumentosController.GerarModelo),
            PolicyNames.PropertyDocumentsManage,
            typeof(Guid),
            typeof(DocumentTemplateType),
            typeof(CancellationToken));
        yield return Policy(
            typeof(DocumentosController),
            nameof(DocumentosController.Download),
            PolicyNames.PropertyDocumentsRead,
            typeof(Guid),
            typeof(Guid),
            typeof(CancellationToken));
    }

    [Theory]
    [MemberData(nameof(ActionPolicyData))]
    public void Action_should_require_expected_policy(
        Type controllerType,
        string actionName,
        Type[] parameterTypes,
        string expectedPolicy)
    {
        var method = controllerType.GetMethod(
            actionName,
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: parameterTypes,
            modifiers: null);

        Assert.NotNull(method);

        var authorizeAttributes = method!.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToArray();
        Assert.NotEmpty(authorizeAttributes);
        Assert.Contains(authorizeAttributes, attribute => attribute.Policy == expectedPolicy);
    }

    private static object[] Policy(
        Type controllerType,
        string actionName,
        string expectedPolicy,
        params Type[] parameterTypes) => new object[] { controllerType, actionName, parameterTypes, expectedPolicy };
}
