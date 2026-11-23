namespace AnalistaPalmaseg.App;

public record DashboardRefreshMessage(int Mes, int Ano);
public record AbrirClienteMessage(string DocumentoPrincipal);
public record NavegarMenuMessage(string Chave);
public record AbrirSeguroRelacionadoMessage(ViewModels.SeguroRelacionado Seguro);
