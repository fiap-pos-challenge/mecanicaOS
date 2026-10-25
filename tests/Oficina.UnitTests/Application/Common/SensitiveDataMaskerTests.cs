using Oficina.Application.Common.Privacy;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Application.Common;

public sealed class SensitiveDataMaskerTests
{
    [Fact]
    public void MaskDocument_HidesTheStartAndEndOfCpfAndTheMiddleOfCnpj()
    {
        Assert.Equal("•••.982.247-••", SensitiveDataMasker.MaskDocument(Document.Create("52998224725").Value));
        Assert.Equal("11.222.•••/••••-81", SensitiveDataMasker.MaskDocument(Document.Create("11222333000181").Value));
    }

    [Fact]
    public void MaskEmailAndPhone_KeepOnlyWhatIdentifiesTheContactToItsOwner()
    {
        Assert.Equal("m•••@exemplo.com", SensitiveDataMasker.MaskEmail(Email.Create("maria@exemplo.com").Value));
        Assert.Equal("••••••4444", SensitiveDataMasker.MaskPhone(Phone.Create("1133334444").Value));
    }
}
