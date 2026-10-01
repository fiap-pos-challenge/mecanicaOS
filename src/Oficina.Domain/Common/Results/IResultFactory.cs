namespace Oficina.Domain.Common.Results;

public interface IResultFactory<TSelf>
    where TSelf : Result
{
    static abstract TSelf Failure(Error error);
}
