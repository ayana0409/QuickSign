export interface LoginRequestDto {
  usernameOrEmail: string;
  password: string;
}

export interface RegisterRequestDto {
  userName: string;
  emailAddress: string;
  password: string;
  appName?: string;
}

export interface AuthSessionResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
}
