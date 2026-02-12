import { AuthApiRepo } from "../../infrastructure/repositories/auth-api-repo";
import { loginUsecase } from "../../application/usecases/auth/login";
import { registerUsecase } from "../../application/usecases/auth/register";

const authRepo = new AuthApiRepo();

export const usecases = {
  login: loginUsecase(authRepo),
  register: registerUsecase(authRepo),
};
