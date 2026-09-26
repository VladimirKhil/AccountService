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
  /** Present only when login was called with includeToken: true. Use to send Authorization: Bearer for non-browser clients. */
  token?: string;
}

export interface UserProfileResponse {
  userId: string;
  username: string;
  displayName?: string;
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

export interface AccountServiceClientOptions {
  /** When set, requests use Authorization: Bearer instead of cookies. Intended for non-browser clients such as Tauri. */
  bearerToken?: string;
}

export class AccountServiceClient {
  private bearerToken?: string;

  constructor(private readonly baseUrl: string, options?: AccountServiceClientOptions) {
    this.bearerToken = options?.bearerToken;
  }

  /**
   * Returns the current bearer token, if one is stored.
   * Use this to persist the token (e.g. to local storage) after login.
   */
  getBearerToken(): string | undefined {
    return this.bearerToken;
  }

  /**
   * Replaces the stored bearer token (e.g. when restoring a session from local storage).
   */
  setBearerToken(token: string | undefined): void {
    this.bearerToken = token;
  }

  /**
   * @param includeToken When true, the server includes the raw JWT in the response body.
   * The client automatically stores it as the bearer token for subsequent requests.
   * Pass true for non-browser clients (e.g. Tauri) that cannot use HttpOnly cookies.
   */
  async loginBySteamAsync(request: SteamLoginRequest, includeToken?: boolean): Promise<AuthResponse> {
    const path = includeToken ? 'auth/steam?includeToken=true' : 'auth/steam';
    const result = await this.post<AuthResponse>(path, request);
    if (result.token) {
      this.bearerToken = result.token;
    }
    return result;
  }

  async getMeAsync(): Promise<UserProfileResponse | null> {
    const response = await fetch(`${this.baseUrl}/api/v1/account/me`, {
      method: 'GET',
      credentials: 'include',
      headers: this.authHeaders(),
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

  private authHeaders(): Record<string, string> {
    if (this.bearerToken) {
      return { Authorization: `Bearer ${this.bearerToken}` };
    }
    return {};
  }

  private async post<T>(path: string, body: unknown): Promise<T> {
    const response = await fetch(`${this.baseUrl}/api/v1/${path}`, {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        ...this.authHeaders(),
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
        ...this.authHeaders(),
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      throw new Error(`Request failed with status ${response.status}`);
    }

    return response.json();
  }
}
