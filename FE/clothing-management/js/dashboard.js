// ═══════════════════════════════════════════
// dashboard.js — Dashboard page render
// ═══════════════════════════════════════════

function renderDashboard() {
  document.getElementById('topbar-actions').innerHTML = '';

  document.getElementById('page-content').innerHTML = `
  <div class="stat-grid">
    <div class="stat-card">
      <div class="stat-icon green"><i class="ti ti-users" aria-hidden="true"></i></div>
      <div class="stat-label">Người dùng</div>
      <div class="stat-value">—</div>
      <div class="stat-sub">Đang tải...</div>
    </div>
    <div class="stat-card">
      <div class="stat-icon blue"><i class="ti ti-shopping-cart" aria-hidden="true"></i></div>
      <div class="stat-label">Đơn hàng</div>
      <div class="stat-value" style="color:var(--info)">—</div>
      <div class="stat-sub">Chưa kết nối</div>
    </div>
    <div class="stat-card">
      <div class="stat-icon orange"><i class="ti ti-hanger" aria-hidden="true"></i></div>
      <div class="stat-label">Sản phẩm</div>
      <div class="stat-value" style="color:var(--warn)">—</div>
      <div class="stat-sub">Chưa kết nối</div>
    </div>
    <div class="stat-card">
      <div class="stat-icon red"><i class="ti ti-building-warehouse" aria-hidden="true"></i></div>
      <div class="stat-label">Tồn kho</div>
      <div class="stat-value" style="color:var(--danger)">—</div>
      <div class="stat-sub">Chưa kết nối</div>
    </div>
  </div>

  <div style="display:grid; grid-template-columns:1fr 1fr; gap:20px">
    <div class="section-card">
      <div class="section-card-header"><h3>Hoạt động gần đây</h3></div>
      <div class="section-card-body">
        <ul class="activity-list">
          <li class="activity-item">
            <div class="activity-dot green"></div>
            <div>
              <div class="activity-text">Đăng nhập thành công</div>
              <div class="activity-time">Vừa xong</div>
            </div>
          </li>
          <li class="activity-item">
            <div class="activity-dot blue"></div>
            <div>
              <div class="activity-text">AuthService kết nối</div>
              <div class="activity-time">Hôm nay</div>
            </div>
          </li>
          <li class="activity-item">
            <div class="activity-dot orange"></div>
            <div>
              <div class="activity-text">UserService đang chờ kết nối</div>
              <div class="activity-time">Port 5002</div>
            </div>
          </li>
        </ul>
      </div>
    </div>

    <div class="section-card">
      <div class="section-card-header"><h3>Thông tin tài khoản</h3></div>
      <div class="section-card-body">
        <table style="width:100%; font-size:13.5px">
          <tr>
            <td style="color:var(--muted); padding:7px 0">Username</td>
            <td style="font-weight:500">${state.user?.username || '—'}</td>
          </tr>
          <tr>
            <td style="color:var(--muted); padding:7px 0">Email</td>
            <td>${state.user?.email || '—'}</td>
          </tr>
          <tr>
            <td style="color:var(--muted); padding:7px 0">Vai trò</td>
            <td>${roleBadge(state.user?.role || 'User')}</td>
          </tr>
          <tr>
            <td style="color:var(--muted); padding:7px 0">Auth Service</td>
            <td><span class="badge active">
              <i class="ti ti-circle-filled" style="font-size:8px"></i> Kết nối
            </span></td>
          </tr>
          <tr>
            <td style="color:var(--muted); padding:7px 0">User Service</td>
            <td><span id="user-service-status">
              <span class="badge inactive">Đang kiểm tra...</span>
            </span></td>
          </tr>
        </table>
      </div>
    </div>
  </div>`;

  // Ping UserService health endpoint
  _pingUserService();
}

async function _pingUserService() {
  try {
    const res = await fetch(`${USER_URL}/api/users/health`);
    const data = res.ok ? await res.json() : null;
    const el = document.getElementById('user-service-status');
    if (el) {
      el.innerHTML = data
        ? `<span class="badge active"><i class="ti ti-circle-filled" style="font-size:8px"></i> Kết nối</span>`
        : `<span class="badge inactive">Không kết nối</span>`;
    }
  } catch {
    const el = document.getElementById('user-service-status');
    if (el) el.innerHTML = `<span class="badge inactive">Không kết nối (port 5002)</span>`;
  }
}