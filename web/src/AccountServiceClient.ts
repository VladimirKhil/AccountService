export interface SteamLoginRequest {
  authTicket: string;
}

export interface UpdateProfileRequest {
  username?: string;
  avatar?: number[];
  gender?: number;
}

export interface AuthResponse {
  userId: string;
}

export interface UserProfileResponse {
  userId: string;
  username: string;
  avatar?: number[];
  gender: number;
}

export interface ValidateSessionRequest {
  token?: string;
}

export interface ValidateSessionResponse {
  isValid: boolean;
  userId?: string;
  username?: string;
}

export class AccountServiceClient {
  constructor(private readonly baseUrl: string) {}

  async loginBySteamAsync(request: SteamLoginRequest): Promise<AuthResponse> {
    return this.post<AuthResponse>('auth/steam', request);
  }

  async getMeAsync(): Promise<UserProfileResponse | null> {
    const response = await fetch(`${this.baseUrl}/api/v1/account/me`, {
      method: 'GET',
      credentials: 'include',
    });

    if (response.status === 404) {
      return null;
    }

    if (!response.ok) {
      throw new Error(`Request failed with status ${response.status}`);
    }

    return response.json();
  }

  async updateMeAsync(request: UpdateProfileRequest): Promise<UserProfileResponse> {
    return this.patch<UserProfileResponse>('account/me', request);
  }

  async requestDeleteMeAsync(): Promise<void> {
    await this.post<unknown>('account/me/delete', {});
  }

  async validateSessionAsync(request: ValidateSessionRequest): Promise<ValidateSessionResponse> {
    return this.post<ValidateSessionResponse>('admin/validate', request);
  }

  private async post<T>(path: string, body: unknown): Promise<T> {
    const response = await fetch(`${this.baseUrl}/api/v1/${path}`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      throw new Error(`Request failed with status ${response.status}`);
    }

    return response.json();
  }

  private async patch<T>(path: string, body: unknown): Promise<T> {
    const response = await fetch(`${this.baseUrl}/api/v1/${path}`, {
      method: 'PATCH',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      throw new Error(`Request failed with status ${response.status}`);
    }

    return response.json();
  }
}
