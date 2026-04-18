/* ============================================
   NEONSTRIKE — MAIN.JS
   ============================================ */

/* --- NAV: cambio de estilo al hacer scroll --- */
const nav = document.querySelector('.nav');

window.addEventListener('scroll', () => {
  if (window.scrollY > 60) {
    nav.style.borderBottomColor = 'rgba(0, 229, 255, 0.3)';
    nav.style.background = 'rgba(7, 9, 15, 0.97)';
  } else {
    nav.style.borderBottomColor = 'rgba(0, 229, 255, 0.2)';
    nav.style.background = 'rgba(7, 9, 15, 0.85)';
  }
});

/* --- NAV: smooth scroll en links internos --- */
document.querySelectorAll('a[href^="#"]').forEach(link => {
  link.addEventListener('click', e => {
    e.preventDefault();
    const target = document.querySelector(link.getAttribute('href'));
    if (!target) return;
    const offset = 80;
    const top = target.getBoundingClientRect().top + window.scrollY - offset;
    window.scrollTo({ top, behavior: 'smooth' });
  });
});

/* --- SCROLL REVEAL --- */
// Añade la clase .reveal a todos los elementos que deben animarse al entrar
const revealSelectors = [
  '.section-label',
  '.section-title',
  '.section-sub',
  '.lore-block',
  '.nexus-grid',
  '.nexus-desc',
  '.char-card',
  '.zone-card',
  '.classified-tag',
];

revealSelectors.forEach(selector => {
  document.querySelectorAll(selector).forEach(el => {
    el.classList.add('reveal');
  });
});

// Observer principal de reveal
const revealObserver = new IntersectionObserver(
  (entries) => {
    entries.forEach((entry, i) => {
      if (entry.isIntersecting) {
        // Delay escalonado para grids
        const siblings = [...entry.target.parentElement.querySelectorAll('.reveal')];
        const index = siblings.indexOf(entry.target);
        const delay = index * 80;

        setTimeout(() => {
          entry.target.classList.add('visible');
        }, delay);

        revealObserver.unobserve(entry.target);
      }
    });
  },
  { threshold: 0.1, rootMargin: '0px 0px -60px 0px' }
);

document.querySelectorAll('.reveal').forEach(el => {
  revealObserver.observe(el);
});

/* --- STAT BARS: animar al entrar en viewport --- */
const statObserver = new IntersectionObserver(
  (entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const fills = entry.target.querySelectorAll('.stat-fill');
        fills.forEach((fill, i) => {
          setTimeout(() => {
            fill.classList.add('animated');
          }, i * 120);
        });
        statObserver.unobserve(entry.target);
      }
    });
  },
  { threshold: 0.3 }
);

document.querySelectorAll('.char-card').forEach(card => {
  statObserver.observe(card);
});

/* --- GLITCH EFFECT en el título del hero --- */
const heroTitle = document.querySelector('.hero-title');

if (heroTitle) {
  const originalHTML = heroTitle.innerHTML;

  const glitchChars = '!<>-_\\/[]{}—=+*^?#________';

  function randomChar() {
    return glitchChars[Math.floor(Math.random() * glitchChars.length)];
  }

  function glitchText(element) {
    const text = element.textContent;
    let iterations = 0;
    const interval = setInterval(() => {
      element.textContent = text
        .split('')
        .map((char, i) => {
          if (char === ' ') return ' ';
          if (i < iterations) return text[i];
          return randomChar();
        })
        .join('');
      iterations += 1.5;
      if (iterations > text.length) {
        clearInterval(interval);
        element.textContent = text;
      }
    }, 30);
  }

  // Glitch suave al hacer hover sobre el título
  heroTitle.addEventListener('mouseenter', () => {
    const lines = heroTitle.querySelectorAll('span');
    lines.forEach((line, i) => {
      setTimeout(() => glitchText(line), i * 100);
    });
  });

  // Glitch automático ocasional (cada 12 segundos)
  setInterval(() => {
    const lines = heroTitle.querySelectorAll('span');
    lines.forEach((line, i) => {
      setTimeout(() => glitchText(line), i * 120);
    });
  }, 12000);
}

/* --- CURSOR personalizado (neon dot) --- */
const cursor = document.createElement('div');
cursor.className = 'cursor-dot';
cursor.style.cssText = `
  position: fixed;
  width: 8px;
  height: 8px;
  background: #00e5ff;
  border-radius: 50%;
  pointer-events: none;
  z-index: 9999;
  transform: translate(-50%, -50%);
  transition: transform 0.1s, width 0.2s, height 0.2s, opacity 0.2s;
  box-shadow: 0 0 12px #00e5ff, 0 0 24px rgba(0,229,255,0.4);
  mix-blend-mode: screen;
`;

const cursorRing = document.createElement('div');
cursorRing.style.cssText = `
  position: fixed;
  width: 32px;
  height: 32px;
  border: 1px solid rgba(0,229,255,0.4);
  border-radius: 50%;
  pointer-events: none;
  z-index: 9998;
  transform: translate(-50%, -50%);
  transition: transform 0.15s, width 0.25s, height 0.25s, opacity 0.2s;
`;

document.body.appendChild(cursor);
document.body.appendChild(cursorRing);

let mouseX = 0, mouseY = 0;
let ringX = 0, ringY = 0;

document.addEventListener('mousemove', e => {
  mouseX = e.clientX;
  mouseY = e.clientY;
  cursor.style.left = mouseX + 'px';
  cursor.style.top  = mouseY + 'px';
});

// Ring sigue con lag
function animateRing() {
  ringX += (mouseX - ringX) * 0.12;
  ringY += (mouseY - ringY) * 0.12;
  cursorRing.style.left = ringX + 'px';
  cursorRing.style.top  = ringY + 'px';
  requestAnimationFrame(animateRing);
}
animateRing();

// Hover sobre elementos interactivos
document.querySelectorAll('a, button, .char-card, .zone-card').forEach(el => {
  el.addEventListener('mouseenter', () => {
    cursor.style.width  = '12px';
    cursor.style.height = '12px';
    cursorRing.style.width  = '48px';
    cursorRing.style.height = '48px';
    cursorRing.style.borderColor = 'rgba(0,229,255,0.7)';
  });
  el.addEventListener('mouseleave', () => {
    cursor.style.width  = '8px';
    cursor.style.height = '8px';
    cursorRing.style.width  = '32px';
    cursorRing.style.height = '32px';
    cursorRing.style.borderColor = 'rgba(0,229,255,0.4)';
  });
});

// Ocultar cursor nativo
document.body.style.cursor = 'none';
document.querySelectorAll('a, button, .char-card, .zone-card').forEach(el => {
  el.style.cursor = 'none';
});

/* --- PARALLAX suave en el hero --- */
const heroBg = document.querySelector('.hero-grid-lines');
const heroGlows = document.querySelectorAll('.hero-glow');

window.addEventListener('scroll', () => {
  const scrollY = window.scrollY;
  if (heroBg) {
    heroBg.style.transform = `translateY(${scrollY * 0.3}px)`;
  }
  heroGlows.forEach((glow, i) => {
    const dir = i % 2 === 0 ? 1 : -1;
    glow.style.transform = `translateY(${scrollY * 0.15 * dir}px)`;
  });
});

/* --- TYPER en el nav status --- */
const statusText = document.querySelector('.status-text');

if (statusText) {
  const messages = [
    'EN DESARROLLO',
    'PRÓXIMAMENTE',
    'BRIGADA ACTIVA',
    'NEXUS DETECTADO',
  ];
  let msgIndex = 0;

  function typeMessage(text, el) {
    el.textContent = '';
    let i = 0;
    const interval = setInterval(() => {
      el.textContent += text[i];
      i++;
      if (i >= text.length) clearInterval(interval);
    }, 60);
  }

  setInterval(() => {
    msgIndex = (msgIndex + 1) % messages.length;
    typeMessage(messages[msgIndex], statusText);
  }, 4000);
}

/* --- CHAR CARDS: efecto de scan al hover --- */
document.querySelectorAll('.char-card').forEach(card => {
  card.addEventListener('mousemove', e => {
    const rect = card.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width) * 100;
    const y = ((e.clientY - rect.top) / rect.height) * 100;
    card.style.setProperty('--mx', `${x}%`);
    card.style.setProperty('--my', `${y}%`);
  });
});