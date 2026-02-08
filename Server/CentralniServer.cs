using Application.Services;
using Core.Enums;
using Core.Models;
using Core.Repositories;
using Infrastructure.Helpers;
using Infrastructure.Mreza;
using Infrastructure.Repositories;
using Server.UI;
using System;
using System.Linq;
using System.Threading;

namespace Server
{
    class CentralniServer
    {
        private const int UDP_STOLOVI_PORT = 4000;  // port tacka 2
        private const int UDP_OTKAZIVANJE_PORT = 4001;
        private const int TCP_RACUN_PORT = 4002; // TACKA 4 
        private const int UDP_PROVERA_REZERVACIJE_PORT = 4003;

        private const int TCP_REGISTER_PORT = 5000; //Tacka 2 
        private const int TCP_READY_PORT = 5001;

        private const int TCP_ORDER_PORT = 15000;
        private const int TCP_DOSTAVA_PORT = 4011;
        private const int UDP_MENADZER_NOTIFY_PORT = 4010;

        static void Main(string[] args)
        {
            Console.WriteLine("Server je pokrenut. Pritisni ENTER za gašenje.");
            ServerVizualizacija.PokreniVizualizaciju();

            // Direktorijum + servisi
            IDirektorijumKlijenata direktorijum = new DirektorijumKlijenata();
            var servisNotifikacija = new ServisNotifikacija(direktorijum);

            IRepozitorijumPorudzbina repoHrane = new RepozitorijumHrane();
            IRepozitorijumPorudzbina repoPica = new RepozitorijumPica();
            var servisPripreme = new ServisSlanjaNaPripremu(direktorijum, repoHrane, repoPica);

            var repozitorijumMenadzera = new RepozitorijumMenadzera(1);

            // TCP obrade
            var tcpObrade = new TcpObrade(servisNotifikacija, servisPripreme, direktorijum);

            MrezniSlusaoci.PokreniTcpSlusalac(TCP_REGISTER_PORT, tcpObrade.ObradiRegistraciju); //Tacka 2 server pokrece tcp slusaac na portu 5000
            MrezniSlusaoci.PokreniTcpSlusalac(TCP_READY_PORT, tcpObrade.ObradiSpremno);
            MrezniSlusaoci.PokreniTcpSlusalac(TCP_ORDER_PORT, tcpObrade.ObradiPorudzbinu);  //TACKA 2 server prica podatke o porudzibnama  TACKA 4 
            MrezniSlusaoci.PokreniTcpSlusalac(TCP_DOSTAVA_PORT, tcpObrade.ObradiDostavljeno);
            MrezniSlusaoci.PokreniTcpSlusalac(TCP_RACUN_PORT, tcpObrade.ObradiRacun); // TAKCA 4

            // UDP obrade
            var udpObrade = new UdpObrade(repozitorijumMenadzera, UDP_MENADZER_NOTIFY_PORT);

            MrezniSlusaoci.PokreniUdpSlusalac(UDP_STOLOVI_PORT, udpObrade.ObradiStolove);  // port tacka 2
            MrezniSlusaoci.PokreniUdpSlusalac(UDP_OTKAZIVANJE_PORT, udpObrade.ObradiOtkazivanje);// port tacka 2
            MrezniSlusaoci.PokreniUdpSlusalac(UDP_PROVERA_REZERVACIJE_PORT, udpObrade.ObradiProveruRezervacije);

            Console.WriteLine("[Server] Svi slusaoci su aktivni.");
            Console.WriteLine("[Server] Svi slusaoci su aktivni.");


            var pokretac = new PokretacKlijenata();
            pokretac.PokreniViseKlijenata(2, TipOsoblja.Konobar);  //Pokretanje klijenata 
            pokretac.PokreniViseKlijenata(1, TipOsoblja.Kuvar);
            pokretac.PokreniViseKlijenata(1, TipOsoblja.Barmen);
            pokretac.PokreniViseKlijenata(1, TipOsoblja.Menadzer);

            Console.ReadLine();
        }
    }
}