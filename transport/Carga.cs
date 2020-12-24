using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace transport
{
    
    public class Carga
    {
        public string carregamento,
        roteirizado,
        Destino,
        codveiculo,
        Placa,
        Veiculo,
        Motorista,
        datamon,
        dtsaida,
        dtretorno,
        dias,
        entregas,
        pedidos,
        Itens,
        km_rodado,
        peso_total,
        volume_total,
        vltotal;

        public Carga() { }
        public Carga(string carregamento, string roteirizado, string destino, string codveiculo, string placa, string veiculo, string motorista, string datamon, string dtsaida, string dtretorno, string dias, string entregas, string itens, string km_rodado, string peso_total, string volume_total,string pedidos,string vltotal)
        {
            this.carregamento = carregamento;
            this.roteirizado = roteirizado;
            this.Destino = destino;
            this.codveiculo = codveiculo;
            this.Placa = placa;
            this.Veiculo = veiculo;
            this.Motorista = motorista;
            this.datamon = datamon;
            this.dtsaida = dtsaida;
            this.dtretorno = dtretorno;
            this.dias = dias;
            this.entregas = entregas;
            this.Itens = itens;
            this.km_rodado = km_rodado;
            this.peso_total = peso_total;
            this.volume_total = volume_total;
            this.pedidos = pedidos;
            this.vltotal = vltotal;
        }
    }
}
