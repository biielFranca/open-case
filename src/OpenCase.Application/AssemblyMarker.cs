namespace OpenCase.Application;

public static class AssemblyMarker
{
    // Garante a dependência física Application -> Domain até os serviços existirem.
    public static readonly System.Reflection.Assembly DomainAssembly =
        typeof(Domain.AssemblyMarker).Assembly;
}
