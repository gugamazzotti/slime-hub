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

            Add("game.tube", "Tube Escape", "Fuga do Tubo", "Fuga del Tubo");
            Add("game.tube.body", "Run, pinch to split, and reach the squeezer while you're big.", "Corra, divida com uma pinça e chegue grande no espremedor.", "Corre, pellizca para dividirte y llega grande al exprimidor.");
            Add("game.critical", "Critical Mass", "Massa Crítica", "Masa Crítica");
            Add("game.critical.body", "Hold to inflate. Release to shrink and dodge the blade.", "Segure para inflar. Solte para murchar e desviar da lâmina.", "Mantén para inflar. Suelta para encoger y esquivar la cuchilla.");
            Add("game.climb", "Sticky Climb", "Escalada Pegajosa", "Escalada Pegajosa");
            Add("game.climb.body", "Pull and release to jump before the acid rises.", "Puxe e solte para saltar antes do ácido subir.", "Tira y suelta para saltar antes de que suba el ácido.");
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
            Add("hud.safe", "Clear!", "Livre!", "¡Libre!");
            Add("hud.recover", "Shake it off!", "Sacode e sobe!", "¡Sacúdete y sube!");
            Add("hud.pull", "Pull and release. Dots show the jump.", "Puxe e solte. Os pontos mostram o salto.", "Tira y suelta. Los puntos muestran el salto.");
            Add("hud.steer", "Drag to steer. Pinch to split.", "Arraste para desviar. Pinça divide.", "Arrastra para girar. Pellizca para dividir.");
            Add("hud.acid", "acid {0} m", "ácido {0} m", "ácido {0} m");

            Add("over.record", "New record!", "Novo recorde!", "¡Nuevo récord!");
            Add("over.points", "{0} points", "{0} pontos", "{0} puntos");
            Add("over.peak", "Peak {0} kg", "Pico {0} kg", "Pico {0} kg");
            Add("over.seconds", "{0} seconds", "{0} segundos", "{0} segundos");
            Add("over.reward", "+{0} biomass", "+{0} biomassa", "+{0} biomasa");
            Add("over.friends", "Friend comparison arrives with the API.", "A comparação com amigos entra com a API.", "La comparación con amigos llega con la API.");
            Add("over.squeezed", "Squeezed!", "Espremido!", "¡Exprimido!");
            Add("over.dissolved", "Dissolved", "Dissolvido", "Disuelto");
            Add("over.centrifuge", "Centrifuge stopped", "Centrífuga parou", "La centrífuga paró");
            Add("over.acid", "Fell in the acid", "Caiu no ácido", "Cayó en el ácido");

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

            Add("mission.tube_40", "Reach 40 kg in Tube Escape", "Chegue a 40 kg no Fuga do Tubo", "Llega a 40 kg en Fuga del Tubo");
            Add("mission.tube_50", "Reach 50 kg in Tube Escape", "Chegue a 50 kg no Fuga do Tubo", "Llega a 50 kg en Fuga del Tubo");
            Add("mission.tube_65", "Reach 65 kg in Tube Escape", "Chegue a 65 kg no Fuga do Tubo", "Llega a 65 kg en Fuga del Tubo");
            Add("mission.crit_30", "Survive 30 s in Critical Mass", "Sobreviva 30 s na Massa Crítica", "Sobrevive 30 s en Masa Crítica");
            Add("mission.crit_45", "Survive 45 s in Critical Mass", "Sobreviva 45 s na Massa Crítica", "Sobrevive 45 s en Masa Crítica");
            Add("mission.crit_60", "Survive 1 minute in Critical Mass", "Sobreviva 1 minuto na Massa Crítica", "Sobrevive 1 minuto en Masa Crítica");
            Add("mission.climb_12", "Climb 12 m in Sticky Climb", "Escale 12 m na Escalada Pegajosa", "Escala 12 m en Escalada Pegajosa");
            Add("mission.climb_20", "Climb 20 m in Sticky Climb", "Escale 20 m na Escalada Pegajosa", "Escala 20 m en Escalada Pegajosa");
            Add("mission.climb_30", "Climb 30 m in Sticky Climb", "Escale 30 m na Escalada Pegajosa", "Escala 30 m en Escalada Pegajosa");
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
