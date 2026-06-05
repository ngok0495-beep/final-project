// ═══════════════════════════════════════════
// nav.js — App boot, Navigation, Page routing
// ═══════════════════════════════════════════

// ── Boot ──────────────────────────────────
function bootApp() {
  document.getElementById('auth-screen').style.display = 'none';
  document.getElementById('app-screen').style.display  = 'block';

  // Sidebar user info
  document.getElementById('sidebar-name').textContent   = state.user?.username || '—';
  document.getElementById('sidebar-role').textContent   = roleLabel(state.user?.role || 'User');
  document.getElementById('sidebar-avatar').textContent = initials(state.user?.username || '');

  // Admin-only nav items
  if (state.user?.role === 'Admin') {
    document.getElementById('nav-users').style.display = '';
  }

  showPage('dashboard');
}

// ── Navigate ──────────────────────────────
// Dùng function thay vì const object để tránh lỗi "before initialization"
// vì các render function nằm ở file khác chưa được load khi PAGES được parse
function getPages() {
  return {
    dashboard: { title: 'Dashboard',          fn: renderDashboard },
    users:     { title: 'Quản lý người dùng', fn: renderUsers },
    profile:   { title: 'Hồ sơ cá nhân',      fn: renderProfile },
  };
}

function showPage(page) {
  state.currentPage = page;

  const p = getPages()[page];
  if (!p) return;

  document.getElementById('topbar-title').textContent = p.title;

  // Highlight active nav item
  document.querySelectorAll('.nav-item').forEach(el => {
    el.classList.remove('active');
    const txt = el.textContent.trim();
    if (
      (page === 'dashboard' && txt.includes('Dashboard')) ||
      (page === 'users'     && txt.includes('Người dùng')) ||
      (page === 'profile'   && txt.includes('Hồ sơ'))
    ) {
      el.classList.add('active');
    }
  });

  p.fn();
}

// ── Coming soon placeholder ───────────────
function showComingSoon(name) {
  document.getElementById('page-content').innerHTML = `
    <div class="empty-state" style="margin-top:80px">
      <i class="ti ti-tool-off" style="font-size:48px; color:var(--hint)"></i>
      <p style="font-size:16px; font-weight:500; margin-bottom:8px">Chức năng ${name}</p>
      <p style="color:var(--muted)">Đang phát triển — sẽ có ở các service tiếp theo.</p>
    </div>`;
  document.getElementById('topbar-actions').innerHTML = '';
}