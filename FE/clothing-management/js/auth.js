// ═══════════════════════════════════════════
// auth.js — Login, Register, Logout, Session
// ═══════════════════════════════════════════

// ── Tab switch ────────────────────────────
function switchTab(tab) {
  document.querySelectorAll('.auth-tab').forEach((el, i) => {
    el.classList.toggle('active',
      (i === 0 && tab === 'login') || (i === 1 && tab === 'register')
    );
  });
  document.getElementById('form-login').style.display    = tab === 'login'    ? '' : 'none';
  document.getElementById('form-register').style.display = tab === 'register' ? '' : 'none';
  hideAlert('alert-auth');
}

// ── Login ─────────────────────────────────
async function doLogin() {
  const email    = document.getElementById('login-email').value.trim();
  const password = document.getElementById('login-password').value;

  if (!email || !password) {
    showAlert('alert-auth', 'Vui lòng điền đầy đủ thông tin.');
    return;
  }

  setLoading('btn-login', true, 'Đang đăng nhập...');
  hideAlert('alert-auth');

  try {
    const res = await fetch(`${AUTH_URL}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
    const data = await res.json();

    if (!res.ok || !data.success) {
      showAlert('alert-auth', data.message || 'Đăng nhập thất bại.');
      return;
    }
    saveSession(data.token, data.user);
    bootApp();
  } catch {
    showAlert('alert-auth', 'Không thể kết nối AuthService (port 5001). Kiểm tra server đã chạy chưa.');
  } finally {
    setLoading('btn-login', false, 'Đăng nhập');
  }
}

// ── Register ──────────────────────────────
async function doRegister() {
  const username = document.getElementById('reg-username').value.trim();
  const email    = document.getElementById('reg-email').value.trim();
  const password = document.getElementById('reg-password').value;

  if (!username || !email || !password) {
    showAlert('alert-auth', 'Vui lòng điền đầy đủ thông tin.');
    return;
  }
  if (password.length < 6) {
    showAlert('alert-auth', 'Mật khẩu phải có ít nhất 6 ký tự.');
    return;
  }

  setLoading('btn-register', true, 'Đang tạo...');
  hideAlert('alert-auth');

  try {
    const res = await fetch(`${AUTH_URL}/api/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, email, password, role: 'User' }),
    });
    const data = await res.json();

    if (!res.ok || !data.success) {
      showAlert('alert-auth', data.message || 'Đăng ký thất bại.');
      return;
    }
    saveSession(data.token, data.user);
    bootApp();
  } catch {
    showAlert('alert-auth', 'Không thể kết nối AuthService (port 5001).');
  } finally {
    setLoading('btn-register', false, 'Tạo tài khoản');
  }
}

// ── Session ───────────────────────────────
function saveSession(token, user) {
  state.token = token;
  state.user  = user;
  localStorage.setItem('cm_token', token);
  localStorage.setItem('cm_user', JSON.stringify(user));
}

function doLogout() {
  state.token = null;
  state.user  = null;
  localStorage.removeItem('cm_token');
  localStorage.removeItem('cm_user');
  document.getElementById('app-screen').style.display  = 'none';
  document.getElementById('auth-screen').style.display = 'flex';
}

// ── Change Password ───────────────────────
async function submitChangePw() {
  const oldPw  = document.getElementById('old-password').value;
  const newPw  = document.getElementById('new-password').value;
  const confPw = document.getElementById('confirm-password').value;

  if (!oldPw || !newPw || !confPw) {
    showAlert('modal-pw-alert', 'Vui lòng điền đầy đủ.');
    return;
  }
  if (newPw.length < 6) {
    showAlert('modal-pw-alert', 'Mật khẩu mới phải có ít nhất 6 ký tự.');
    return;
  }
  if (newPw !== confPw) {
    showAlert('modal-pw-alert', 'Mật khẩu xác nhận không khớp.');
    return;
  }

  setLoading('btn-change-pw', true, 'Đang cập nhật...');
  hideAlert('modal-pw-alert');

  try {
    const res = await fetch(`${AUTH_URL}/api/auth/change-password`, {
      method: 'PUT',
      headers: headers(),
      body: JSON.stringify({ oldPassword: oldPw, newPassword: newPw }),
    });
    const data = await res.json();

    if (res.ok && data.success) {
      closeModal('modal-change-pw');
      ['old-password', 'new-password', 'confirm-password'].forEach(
        id => (document.getElementById(id).value = '')
      );
      toast('Đổi mật khẩu thành công!', 'success');
    } else {
      showAlert('modal-pw-alert', data.message || 'Đổi mật khẩu thất bại.');
    }
  } catch {
    showAlert('modal-pw-alert', 'Lỗi kết nối AuthService.');
  } finally {
    setLoading('btn-change-pw', false, '<i class="ti ti-lock"></i> Đổi mật khẩu');
  }
}