using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace SlimeHub.Core
{
    public static class Loc
    {
        enum Lang
        {
            En = 0,
            Pt = 1,
            Es = 2
        }

        static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>(160);
        static Lang current = Lang.En;

        static Loc()
        {
            Add("boot.subtitle", "a living terrarium", "terrário vivo", "un terrario vivo");

            Add("hub.title", "Terrarium", "Terrário", "Terrario");
            Add("hub.biomass", "{0}  biomass", "{0}  biomassa", "{0}  biomasa");
            Add("hub.sound.on", "Sound", "Som", "Sonido");
            Add("hub.sound.off", "Mute", "Mudo", "Mudo");
            Add("hub.haptic.on", "Haptics", "Vibra", "Vibra");
            Add("hub.haptic.off", "Off", "Sem vibra", "Sin vibra");
            Add("hub.hint", "Drag the slime to interact!", "Arraste o slime para interagir!", "¡Arrastra el slime para interactuar!");
            Add("hub.hint.editor", "Drag the slime. WASD tilts. Space splits in minigames.", "Arraste o slime. WASD inclina. Espaço divide nos minigames.", "Arrastra el slime. WASD inclina. Espacio divide en los minijuegos.");
            Add("hub.customize", "Customize", "Customizar", "Personalizar");
            Add("hub.play", "Play", "Jogar", "Jugar");
            Add("hub.games", "Minigames", "Minigames", "Minijuegos");
            Add("hub.missions", "Missions", "Missões", "Misiones");
            Add("hub.fusion", "Fusion", "Fusão", "Fusión");
            Add("hub.close", "Close", "Fechar", "Cerrar");

            Add("shop.color", "Color", "Cor", "Color");
            Add("shop.texture", "Texture", "Textura", "Textura");
            Add("shop.core", "Core", "Núcleo", "Núcleo");
            Add("shop.viscosity", "Viscosity", "Viscosidade", "Viscosidad");
            Add("shop.viscosity.note", "More liquid slides and wobbles. More dense holds its shape and sticks better while climbing.", "Mais líquido escorrega e balança. Mais denso segura o formato e gruda melhor na escalada.", "Más líquido resbala y se bambolea. Más denso mantiene la forma y se pega mejor al escalar.");
            Add("shop.using", "  using", "  usando", "  en uso");
            Add("shop.price", " biomass", " biomassa", " biomasa");

            Add("game.hop", "Jelly Hop", "Pulo de Gelatina", "Salto de Gelatina");
            Add("game.hop.body", "Slide sideways. The slime bounces by itself. Don't miss a pad.", "Deslize para os lados. O slime quica sozinho. Não erre a plataforma.", "Desliza a los lados. El slime rebota solo. No falles la plataforma.");
            Add("game.merge", "Slime Merge", "Fusão", "Fusión");
            Add("game.merge.body", "Drop slimes. Two of the same size melt into a bigger one.", "Solte slimes. Dois do mesmo tamanho viram um maior.", "Suelta slimes. Dos del mismo tamaño se vuelven uno mayor.");
            Add("game.gulp", "Gulp", "Gula", "Glotón");
            Add("game.gulp.body", "Drag into the green snacks. Stay away from the red ones.", "Arraste até os petiscos verdes. Fuja dos vermelhos.", "Arrastra hacia las golosinas verdes. Aléjate de las rojas.");
            Add("games.record", "Best  {0}", "Recorde  {0}", "Récord  {0}");
            Add("games.enter", "Enter", "Entrar", "Entrar");

            Add("missions.empty", "No missions today.", "Sem missões hoje.", "No hay misiones hoy.");
            Add("missions.claimed", "   claimed", "   resgatada", "   cobrada");
            Add("missions.claim", "Claim {0} biomass", "Resgatar {0} biomassa", "Cobrar {0} biomasa");

            Add("fusion.note", "Cross your slime with a friend's. The palette comes from mixing the two. The API lands on this same button later.", "Cruze seu slime com o de um amigo. A paleta sai da mistura dos dois. A API entra nesse mesmo botão depois.", "Cruza tu slime con el de un amigo. La paleta sale de mezclar los dos. La API entra en este mismo botón después.");
            Add("fusion.placeholder", "Friend code", "Código do amigo", "Código del amigo");
            Add("fusion.button", "Fuse  {0} biomass", "Fundir  {0} biomassa", "Fusionar  {0} biomasa");
            Add("fusion.empty", "No fusions yet.", "Nenhuma fusão ainda.", "Ninguna fusión todavía.");
            Add("fusion.yours", "Your fusions", "Suas fusões", "Tus fusiones");
            Add("fusion.anon", "Anonymous fusion", "Fusão anônima", "Fusión anónima");
            Add("fusion.with", "Fusion {0}", "Fusão {0}", "Fusión {0}");
            Add("fusion.broke", "Not enough biomass.", "Biomassa insuficiente.", "Biomasa insuficiente.");

            Add("shell.pause", "Pause", "Pausa", "Pausa");
            Add("shell.resume", "Resume", "Continuar", "Continuar");
            Add("shell.home", "Terrarium", "Terrário", "Terrario");
            Add("shell.retry", "Again", "De novo", "Otra vez");
            Add("shell.go", "GO", "VAI", "YA");

            Add("hud.ready", "get ready", "prepare-se", "prepárate");
            Add("hud.peak", "peak {0} kg", "pico {0} kg", "pico {0} kg");
            Add("hud.hold", "hold to inflate", "segure para inflar", "mantén para inflar");
            Add("hud.pull", "Pull and release", "Puxe e solte", "Tira y suelta");
            Add("hud.hop", "Slide to steer", "Deslize para guiar", "Desliza para guiar");
            Add("hud.merge", "Release to drop", "Solte para cair", "Suelta para caer");
            Add("hud.gulp", "Eat green, dodge red", "Coma o verde, fuja do vermelho", "Come lo verde, huye de lo rojo");

            Add("over.record", "New record!", "Novo recorde!", "¡Nuevo récord!");
            Add("over.points", "{0} points", "{0} pontos", "{0} puntos");
            Add("over.peak", "Peak {0} kg", "Pico {0} kg", "Pico {0} kg");
            Add("over.seconds", "{0} seconds", "{0} segundos", "{0} segundos");
            Add("over.reward", "+{0} biomass", "+{0} biomassa", "+{0} biomasa");
            Add("over.lands", "{0} bounces", "{0} quiques", "{0} rebotes");
            Add("over.eaten", "{0} snacks", "{0} petiscos", "{0} golosinas");
            Add("over.hopwin", "Boing!", "Boing!", "¡Boing!");
            Add("over.hoplose", "Missed the pad", "Errou a plataforma", "Fallaste la plataforma");
            Add("over.mergewin", "Big slime!", "Slime enorme!", "¡Slime enorme!");
            Add("over.mergelose", "The jar overflowed", "O pote transbordou", "El frasco se desbordó");
            Add("over.gulpwin", "All gobbled up", "Comeu tudo", "Te lo comiste todo");
            Add("over.gulplose", "Bitten by red", "Mordeu o vermelho", "Te mordió el rojo");

            Add("item.color_menta", "Mint", "Menta", "Menta");
            Add("item.color_cereja", "Cherry", "Cereja", "Cereza");
            Add("item.color_oceano", "Ocean", "Oceano", "Océano");
            Add("item.color_uva", "Grape", "Uva", "Uva");
            Add("item.color_lava", "Lava", "Lava", "Lava");
            Add("item.color_fantasma", "Ghost", "Fantasma", "Fantasma");
            Add("item.tex_puro", "Plain", "Puro", "Puro");
            Add("item.tex_glitter", "Glitter", "Glitter", "Glitter");
            Add("item.tex_radioativo", "Radioactive", "Radioativo", "Radioactivo");
            Add("item.tex_neon", "Neon", "Neon", "Neón");
            Add("item.tex_metalico", "Metallic", "Metálico", "Metálico");
            Add("item.core_none", "No core", "Sem núcleo", "Sin núcleo");
            Add("item.core_duck", "Duck", "Pato", "Pato");
            Add("item.core_skull", "Skull", "Caveirinha", "Calavera");
            Add("item.core_galaxy", "Galaxy", "Galáxia", "Galaxia");

            Add("mission.hop_6", "Bounce on 6 pads", "Quique em 6 plataformas", "Rebota en 6 plataformas");
            Add("mission.hop_8", "Bounce on 8 pads", "Quique em 8 plataformas", "Rebota en 8 plataformas");
            Add("mission.hop_10", "Bounce on 10 pads", "Quique em 10 plataformas", "Rebota en 10 plataformas");
            Add("mission.merge_40", "Score 40 in Slime Merge", "Faça 40 pontos na Fusão", "Haz 40 puntos en Fusión");
            Add("mission.merge_80", "Score 80 in Slime Merge", "Faça 80 pontos na Fusão", "Haz 80 puntos en Fusión");
            Add("mission.merge_120", "Score 120 in Slime Merge", "Faça 120 pontos na Fusão", "Haz 120 puntos en Fusión");
            Add("mission.gulp_4", "Eat 4 snacks", "Absorva 4 petiscos", "Come 4 golosinas");
            Add("mission.gulp_6", "Eat 6 snacks", "Absorva 6 petiscos", "Come 6 golosinas");
            Add("mission.gulp_8", "Eat 8 snacks", "Absorva 8 petiscos", "Come 8 golosinas");
        }

        public static void ApplySystem()
        {
            SystemLanguage language = Application.systemLanguage;
            if (language == SystemLanguage.Portuguese)
                current = Lang.Pt;
            else if (language == SystemLanguage.Spanish)
                current = Lang.Es;
            else if (language == SystemLanguage.English)
                current = Lang.En;
            else if (language == SystemLanguage.Unknown)
                current = FromIso(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            else
                current = Lang.En;
        }

        public static bool Has(string key)
        {
            return key != null && table.ContainsKey(key);
        }

        public static string Get(string key)
        {
            if (key != null && table.TryGetValue(key, out string[] row))
            {
                string value = row[(int)current];
                if (!string.IsNullOrEmpty(value))
                    return value;
                if (!string.IsNullOrEmpty(row[0]))
                    return row[0];
            }

            return key ?? "";
        }

        public static string Format(string key, params object[] args)
        {
            string pattern = Get(key);
            if (args == null || args.Length == 0)
                return pattern;
            return string.Format(CultureInfo.InvariantCulture, pattern, args);
        }

        public static string Or(string key, string fallback)
        {
            return Has(key) ? Get(key) : (string.IsNullOrEmpty(fallback) ? key : fallback);
        }

        public static string Outcome(bool record, int reward, params string[] lines)
        {
            var builder = new StringBuilder();
            if (record)
                builder.Append(Get("over.record")).Append('\n');
            if (lines != null)
            {
                for (int i = 0; i < lines.Length; i++)
                    builder.Append(lines[i]).Append('\n');
            }

            builder.Append(Format("over.reward", reward));
            return builder.ToString();
        }

        static Lang FromIso(string iso)
        {
            if (iso == "pt")
                return Lang.Pt;
            if (iso == "es")
                return Lang.Es;
            return Lang.En;
        }

        static void Add(string key, string en, string pt, string es)
        {
            table[key] = new[] { en, pt, es };
        }
    }
}
