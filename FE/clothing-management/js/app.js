// ═══════════════════════════════════════════
// app.js — App entry point, keyboard events,
//           session auto-restore
// ═══════════════════════════════════════════

// ── Keyboard shortcuts ────────────────────
window.addEventListener('keydown', e => {
  // Enter to submit auth forms
  if (e.key === 'Enter') {
    const authVisible = document.getElementById('auth-screen').style.display !== 'none';
    if (authVisible) {
      const isRegister = document.getElementById('form-register').style.display !== 'none';
      isRegister ? doRegister() : doLogin();
    }
  }

  // Escape to close modals
  if (e.key === 'Escape') {
    document.querySelectorAll('.modal-backdrop.show').forEach(m => m.classList.remove('show'));
  }
});

// ── Auto-restore session on load ──────────
if (state.token && state.user) {
  bootApp();
} else {
  document.getElementById('auth-screen').style.display = 'flex';
}