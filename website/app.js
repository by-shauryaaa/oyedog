/* ============================================================
   🐶 OYE DOG — LANDING PAGE SCRIPTS
   ============================================================ */

// ⚙️ CONFIGURATION: Installer download URL
const DOWNLOAD_URL = "https://drive.google.com/file/d/1o0qZ8vxxRZ21yw008sOS16XRbpHxCs7u/view?usp=sharing";

// Sprite Configuration
const SPRITE_CONFIG = {
    idle: { frames: 5, name: "Idle Companion 🐾", message: "Woof! Hanging out on your desktop to keep you company :)" },
    walking: { frames: 8, name: "Walking Stroll 🚶", message: "Walking across the screen for my morning greeting!" },
    food: { frames: 5, name: "Snack Time 🍖", message: "kuch khaya? Crunch crunch kibbles!" },
    water: { frames: 5, name: "Hydrate Alert 💧", message: "paani pi le! Stay refreshed!" },
    sleep: { frames: 5, name: "Sleep & Rest 🌙", message: "abe soja ab! Time to recharge for tomorrow." },
    rest: { frames: 5, name: "Break / Stretch 🧘", message: "Stretch your paws and take a quick break." },
    barca: { frames: 5, name: "FC Barcelona ⚽", message: "Força Barça! Match day reminders active!" },
    f1: { frames: 5, name: "Formula 1 🏎️", message: "Lights out and away we go! Race session alert!" },
    birthday_walk: { frames: 8, name: "Birthday Celebration 🎂", message: "🎂 Celebration party mode! Fast laps and goals!" }
};

// Hero Mascot State
let heroVariant = "idle";
let heroFrame = 0;

// Playground Mascot State
let playgroundVariant = "idle";
let playgroundFrame = 0;

// Initialize on DOM Ready
document.addEventListener("DOMContentLoaded", () => {
    setupDownloadButtons();
    setupHeroMascot();
    setupPlayground();
    setupConfetti();
});

// Setup Download Links
function setupDownloadButtons() {
    const downloadBtns = document.querySelectorAll(".btn-download-app");
    downloadBtns.forEach(btn => {
        btn.addEventListener("click", (e) => {
            burstConfetti(e.clientX, e.clientY, 40);
            if (DOWNLOAD_URL && DOWNLOAD_URL.startsWith("http")) {
                window.open(DOWNLOAD_URL, "_blank", "noopener,noreferrer");
                e.preventDefault();
            }
        });
    });
}

// Hero Mascot Animation & Click Interaction
function setupHeroMascot() {
    const heroImg = document.getElementById("hero-dog-sprite");
    const bubbleMsg = document.getElementById("hero-speech-msg");
    const dogBox = document.getElementById("hero-dog-box");

    // 8 FPS Animation Loop (125ms)
    setInterval(() => {
        const config = SPRITE_CONFIG[heroVariant];
        heroFrame = (heroFrame + 1) % config.frames;
        if (heroImg) {
            heroImg.src = `assets/sprites/${heroVariant}_${heroFrame}.png`;
        }
    }, 125);

    // Click Dog -> Pet reaction + Confetti burst!
    if (dogBox) {
        dogBox.addEventListener("click", (e) => {
            burstConfetti(e.clientX, e.clientY, 45);
            
            // Random pet reaction
            const reactions = [
                "🐾 *happy tail wags & barks* Woof! Thanks for the pets! ❤️",
                "🍖 Crunch crunch! Yum, delicious treats! 😋",
                "🚶 Ready to stroll across your screen!",
                "🏎️ Vroom! Ready for the next F1 Grand Prix! 🏁",
                "⚽ Visca el Barça! Match alerts are set!"
            ];
            const chosen = reactions[Math.floor(Math.random() * reactions.length)];
            if (bubbleMsg) {
                bubbleMsg.textContent = chosen;
            }
        });
    }
}

// Playground Switcher
function setupPlayground() {
    const playImg = document.getElementById("playground-sprite");
    const playCaption = document.getElementById("playground-caption");
    const selectorBtns = document.querySelectorAll(".btn-sprite-select");

    // Playground Animation Loop
    setInterval(() => {
        const config = SPRITE_CONFIG[playgroundVariant];
        playgroundFrame = (playgroundFrame + 1) % config.frames;
        if (playImg) {
            playImg.src = `assets/sprites/${playgroundVariant}_${playgroundFrame}.png`;
        }
    }, 125);

    // Handle Tab Buttons
    selectorBtns.forEach(btn => {
        btn.addEventListener("click", () => {
            selectorBtns.forEach(b => b.classList.remove("active"));
            btn.classList.add("active");

            const variant = btn.dataset.variant;
            if (SPRITE_CONFIG[variant]) {
                playgroundVariant = variant;
                playgroundFrame = 0;
                if (playCaption) {
                    playCaption.textContent = SPRITE_CONFIG[variant].message;
                }
            }
        });
    });
}

// ============================================================
// 🎊 CANVAS CONFETTI SYSTEM
// ============================================================
let confettiParticles = [];
const CONFETTI_COLORS = ["#FF4B6E", "#FFD700", "#32D2FF", "#96F03C", "#D25AFF", "#FF763B"];

function setupConfetti() {
    const canvas = document.getElementById("confetti-canvas");
    if (!canvas) return;
    const ctx = canvas.getContext("2d");

    function resizeCanvas() {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
    }
    resizeCanvas();
    window.addEventListener("resize", resizeCanvas);

    function updateConfetti() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        for (let i = confettiParticles.length - 1; i >= 0; i--) {
            const p = confettiParticles[i];
            p.x += p.vx;
            p.y += p.vy;
            p.vy += p.gravity;
            p.rotation += p.rotSpeed;
            p.opacity -= p.fade;

            if (p.opacity <= 0 || p.y > canvas.height + 20) {
                confettiParticles.splice(i, 1);
                continue;
            }

            ctx.save();
            ctx.translate(p.x, p.y);
            ctx.rotate(p.rotation);
            ctx.fillStyle = p.color;
            ctx.globalAlpha = p.opacity;
            ctx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size * 1.5);
            ctx.restore();
        }

        requestAnimationFrame(updateConfetti);
    }
    requestAnimationFrame(updateConfetti);
}

function burstConfetti(x, y, count = 40) {
    for (let i = 0; i < count; i++) {
        const angle = Math.random() * Math.PI * 2;
        const speed = Math.random() * 8 + 3;
        confettiParticles.push({
            x: x || window.innerWidth / 2,
            y: y || window.innerHeight / 2,
            vx: Math.cos(angle) * speed,
            vy: Math.sin(angle) * speed - 4,
            gravity: 0.22,
            size: Math.random() * 8 + 6,
            color: CONFETTI_COLORS[Math.floor(Math.random() * CONFETTI_COLORS.length)],
            rotation: Math.random() * Math.PI,
            rotSpeed: (Math.random() - 0.5) * 0.2,
            opacity: 1.0,
            fade: Math.random() * 0.012 + 0.006
        });
    }
}
